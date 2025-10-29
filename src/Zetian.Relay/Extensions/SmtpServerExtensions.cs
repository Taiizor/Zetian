using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.Models.EventArgs;
using Zetian.Relay.Abstractions;
using Zetian.Relay.Configuration;
using Zetian.Relay.Enums;
using Zetian.Relay.Models;
using Zetian.Relay.Services;

namespace Zetian.Relay.Extensions
{
    /// <summary>
    /// Extension methods for adding relay functionality to SMTP server
    /// </summary>
    public static class SmtpServerExtensions
    {
        private const string RelayServiceKey = "Zetian.Relay.Service";

        /// <summary>
        /// Enables relay functionality on the SMTP server
        /// </summary>
        public static ISmtpServer EnableRelay(
            this ISmtpServer server,
            RelayConfiguration? configuration = null)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            configuration ??= new RelayConfiguration();

            ILogger<RelayService>? logger = server.Configuration.LoggerFactory?.CreateLogger<RelayService>();
            RelayService relayService = new(configuration, logger: logger);

            // Store relay service in server properties
            server.Configuration.Properties[RelayServiceKey] = relayService;

            // Subscribe to message received event
            server.MessageReceived += async (sender, e) => await OnMessageReceivedAsync(relayService, e);

            return server;
        }

        /// <summary>
        /// Enables relay with configuration action
        /// </summary>
        public static ISmtpServer EnableRelay(
            this ISmtpServer server,
            Action<RelayConfiguration> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            RelayConfiguration configuration = new();
            configure(configuration);

            return server.EnableRelay(configuration);
        }

        /// <summary>
        /// Adds a smart host for relay
        /// </summary>
        public static ISmtpServer AddSmartHost(
            this ISmtpServer server,
            string host,
            int port = 25,
            NetworkCredential? credentials = null)
        {
            RelayService? relayService = GetRelayService(server);
            if (relayService == null)
            {
                // Enable relay with default configuration
                server.EnableRelay();
                relayService = GetRelayService(server);
            }

            SmartHostConfiguration smartHost = new()
            {
                Host = host,
                Port = port,
                Credentials = credentials,
                UseTls = port is 465 or 587,
                UseStartTls = port == 587
            };

            // This is a simplified approach - in production you'd modify the configuration
            RelayConfiguration? config = GetRelayConfiguration(server);
            if (config != null)
            {
                if (config.DefaultSmartHost == null)
                {
                    config.DefaultSmartHost = smartHost;
                }
                else
                {
                    config.SmartHosts.Add(smartHost);
                }
            }

            return server;
        }

        /// <summary>
        /// Sets relay domains
        /// </summary>
        public static ISmtpServer SetRelayDomains(
            this ISmtpServer server,
            params string[] domains)
        {
            RelayConfiguration? config = GetRelayConfiguration(server);
            if (config == null)
            {
                server.EnableRelay();
                config = GetRelayConfiguration(server);
            }

            if (config != null)
            {
                config.RelayDomains.Clear();
                foreach (string domain in domains)
                {
                    config.RelayDomains.Add(domain);
                }
            }

            return server;
        }

        /// <summary>
        /// Sets local domains (not relayed)
        /// </summary>
        public static ISmtpServer SetLocalDomains(
            this ISmtpServer server,
            params string[] domains)
        {
            RelayConfiguration? config = GetRelayConfiguration(server);
            if (config == null)
            {
                server.EnableRelay();
                config = GetRelayConfiguration(server);
            }

            if (config != null)
            {
                config.LocalDomains.Clear();
                foreach (string domain in domains)
                {
                    config.LocalDomains.Add(domain);
                }
            }

            return server;
        }

        /// <summary>
        /// Adds relay network (IPs allowed to relay without auth)
        /// </summary>
        public static ISmtpServer AddRelayNetwork(
            this ISmtpServer server,
            IPAddress network)
        {
            RelayConfiguration? config = GetRelayConfiguration(server);
            if (config == null)
            {
                server.EnableRelay();
                config = GetRelayConfiguration(server);
            }

            config?.RelayNetworks.Add(network);

            return server;
        }

        /// <summary>
        /// Starts the SMTP server with relay enabled
        /// </summary>
        public static async Task<RelayService> StartWithRelayAsync(
            this ISmtpServer server,
            RelayConfiguration? configuration = null,
            CancellationToken cancellationToken = default)
        {
            if (configuration != null)
            {
                server.EnableRelay(configuration);
            }

            RelayService? relayService = GetRelayService(server);
            if (relayService == null)
            {
                server.EnableRelay();
                relayService = GetRelayService(server);
            }

            // Start SMTP server
            await server.StartAsync(cancellationToken);

            // Start relay service
            if (relayService != null)
            {
                await relayService.StartAsync(cancellationToken);
            }

            return relayService!;
        }

        /// <summary>
        /// Gets the relay service from the server
        /// </summary>
        public static RelayService? GetRelayService(this ISmtpServer server)
        {
            if (server?.Configuration?.Properties?.TryGetValue(RelayServiceKey, out object? service) == true)
            {
                return service as RelayService;
            }

            return null;
        }

        /// <summary>
        /// Gets relay queue statistics
        /// </summary>
        public static async Task<RelayQueueStatistics?> GetRelayStatisticsAsync(
            this ISmtpServer server,
            CancellationToken cancellationToken = default)
        {
            RelayService? relayService = GetRelayService(server);
            if (relayService?.Queue != null)
            {
                return await relayService.Queue.GetStatisticsAsync(cancellationToken);
            }

            return null;
        }

        /// <summary>
        /// Queues a message for relay manually
        /// </summary>
        public static async Task<IRelayMessage?> QueueForRelayAsync(
            this ISmtpServer server,
            ISmtpMessage message,
            ISmtpSession session,
            RelayPriority priority = RelayPriority.Normal,
            CancellationToken cancellationToken = default)
        {
            RelayService? relayService = GetRelayService(server);
            if (relayService != null)
            {
                return await relayService.QueueMessageAsync(
                    message,
                    session,
                    priority,
                    cancellationToken);
            }

            return null;
        }

        private static RelayConfiguration? GetRelayConfiguration(ISmtpServer server)
        {
            // In a real implementation, you'd store and retrieve the configuration properly
            // For now, we'll try to get it from the relay service
            RelayService? relayService = GetRelayService(server);
            if (relayService != null)
            {
                // The configuration is private in our current implementation
                // In production, you'd expose it as a property
                return new RelayConfiguration();
            }

            return null;
        }

        private static async Task OnMessageReceivedAsync(RelayService relayService, MessageEventArgs e)
        {
            try
            {
                // Check if message should be relayed
                if (ShouldRelayMessage(e.Message, e.Session))
                {
                    // Queue message for relay
                    await relayService.QueueMessageAsync(
                        e.Message,
                        e.Session,
                        DeterminePriority(e.Message));

                    // Cancel local delivery
                    e.Cancel = false; // Allow local processing to continue
                    // In production, you might want to cancel local delivery
                    // e.Cancel = true;
                    // e.Response = new SmtpResponse(250, "Message queued for relay");
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the message reception
                ILogger? logger = e.Session.Properties.TryGetValue("Logger", out object? loggerObj)
                    ? loggerObj as ILogger
                    : null;

                logger?.LogError(ex, "Error processing message for relay");
            }
        }

        private static bool ShouldRelayMessage(ISmtpMessage message, ISmtpSession session)
        {
            // This is a simplified check - in production you'd have more complex logic
            // based on configuration, authentication, recipient domains, etc.

            // For now, relay if:
            // 1. Session is authenticated
            // 2. Or message has external recipients

            if (session.IsAuthenticated)
            {
                return true;
            }

            // Check if any recipient is external
            foreach (MailAddress recipient in message.Recipients)
            {
                // Simple check - in production you'd check against local domains
                if (!recipient.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static RelayPriority DeterminePriority(ISmtpMessage message)
        {
            // Determine priority based on message properties
            if (message.Priority == MailPriority.High)
            {
                return RelayPriority.High;
            }

            if (message.Priority == MailPriority.Low)
            {
                return RelayPriority.Low;
            }

            // Check headers for priority indicators
            string? priority = message.GetHeader("X-Priority");
            if (priority != null)
            {
                if (priority.Contains('1') || priority.Contains("urgent", StringComparison.OrdinalIgnoreCase))
                {
                    return RelayPriority.Urgent;
                }
                if (priority.Contains('2') || priority.Contains("high", StringComparison.OrdinalIgnoreCase))
                {
                    return RelayPriority.High;
                }
                if (priority.Contains('4') || priority.Contains('5') ||
                    priority.Contains("low", StringComparison.OrdinalIgnoreCase))
                {
                    return RelayPriority.Low;
                }
            }

            return RelayPriority.Normal;
        }
    }
}