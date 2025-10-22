using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.Models;
using Zetian.Protocol;
using Zetian.RateLimiting;

namespace Zetian.Extensions
{
    /// <summary>
    /// Extension methods for SMTP server
    /// </summary>
    public static class SmtpServerExtensions
    {
        /// <summary>
        /// Adds rate limiting to the SMTP server
        /// </summary>
        public static ISmtpServer AddRateLimiting(this ISmtpServer server, IRateLimiter rateLimiter)
        {
            ArgumentNullException.ThrowIfNull(server);

            ArgumentNullException.ThrowIfNull(rateLimiter);

            server.SessionCreated += async (sender, e) =>
            {
                if (e.Session.RemoteEndPoint is IPEndPoint ipEndPoint)
                {
                    if (!await rateLimiter.IsAllowedAsync(ipEndPoint.Address))
                    {
                        // Close the session if rate limit exceeded
                        // This would require adding a Close method to ISmtpSession
                        // For now, we'll track it in properties
                        e.Session.Properties["RateLimitExceeded"] = true;
                    }
                }
            };

            server.MessageReceived += async (sender, e) =>
            {
                if (e.Session.RemoteEndPoint is IPEndPoint ipEndPoint)
                {
                    if (e.Session.Properties.TryGetValue("RateLimitExceeded", out object? exceeded) &&
                        exceeded is bool && (bool)exceeded)
                    {
                        e.Cancel = true;
                        e.Response = new SmtpResponse(421, "Rate limit exceeded. Please try again later.");
                        return;
                    }

                    await rateLimiter.RecordRequestAsync(ipEndPoint.Address);
                }
            };

            return server;
        }

        /// <summary>
        /// Adds simple rate limiting with in-memory storage
        /// </summary>
        public static ISmtpServer AddRateLimiting(this ISmtpServer server, RateLimitConfiguration configuration)
        {
            return server.AddRateLimiting(new InMemoryRateLimiter(configuration));
        }

        /// <summary>
        /// Adds a message filter
        /// </summary>
        public static ISmtpServer AddMessageFilter(this ISmtpServer server, Func<ISmtpMessage, bool> filter)
        {
            ArgumentNullException.ThrowIfNull(server);

            ArgumentNullException.ThrowIfNull(filter);

            server.MessageReceived += (sender, e) =>
            {
                if (!filter(e.Message))
                {
                    e.Cancel = true;
                    e.Response = new SmtpResponse(550, "Message rejected by filter");
                }
            };

            return server;
        }

        /// <summary>
        /// Adds spam filtering based on sender
        /// </summary>
        public static ISmtpServer AddSpamFilter(this ISmtpServer server, IEnumerable<string> blacklistedDomains)
        {
            HashSet<string> blacklist = new(blacklistedDomains, StringComparer.OrdinalIgnoreCase);

            return server.AddMessageFilter(message =>
            {
                if (message.From != null)
                {
                    string domain = message.From.Host;
                    return !blacklist.Contains(domain);
                }
                return true;
            });
        }

        /// <summary>
        /// Adds size filtering
        /// </summary>
        public static ISmtpServer AddSizeFilter(this ISmtpServer server, long maxSizeBytes)
        {
            return server.AddMessageFilter(message => message.Size <= maxSizeBytes);
        }

        /// <summary>
        /// Saves all messages to a directory
        /// </summary>
        public static ISmtpServer SaveMessagesToDirectory(this ISmtpServer server, string directory)
        {
            ArgumentNullException.ThrowIfNull(server);

            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("Directory cannot be empty", nameof(directory));
            }

            Directory.CreateDirectory(directory);

            server.MessageReceived += async (sender, e) =>
            {
                string fileName = $"{e.Message.Id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.eml";
                string filePath = Path.Combine(directory, fileName);

                await e.Message.SaveToFileAsync(filePath);
            };

            return server;
        }

        /// <summary>
        /// Adds message logging
        /// </summary>
        public static ISmtpServer LogMessages(this ISmtpServer server, Action<ISmtpMessage> logger)
        {
            ArgumentNullException.ThrowIfNull(server);

            ArgumentNullException.ThrowIfNull(logger);

            server.MessageReceived += (sender, e) => logger(e.Message);

            return server;
        }

        /// <summary>
        /// Adds message forwarding
        /// </summary>
        public static ISmtpServer ForwardMessages(this ISmtpServer server,
            Func<ISmtpMessage, Task<bool>> forwarder)
        {
            ArgumentNullException.ThrowIfNull(server);

            ArgumentNullException.ThrowIfNull(forwarder);

            server.MessageReceived += async (sender, e) =>
            {
                try
                {
                    bool success = await forwarder(e.Message);
                    if (!success)
                    {
                        // Optionally handle forwarding failure
                        e.Session.Properties["ForwardingFailed"] = true;
                    }
                }
                catch (Exception ex)
                {
                    e.Session.Properties["ForwardingError"] = ex.Message;
                }
            };

            return server;
        }

        /// <summary>
        /// Adds recipient validation
        /// </summary>
        public static ISmtpServer AddRecipientValidation(this ISmtpServer server,
            Func<MailAddress, bool> validator)
        {
            ArgumentNullException.ThrowIfNull(server);

            ArgumentNullException.ThrowIfNull(validator);

            server.MessageReceived += (sender, e) =>
            {
                List<MailAddress> invalidRecipients = e.Message.Recipients
                    .Where(r => !validator(r))
                    .ToList();

                if (invalidRecipients.Any())
                {
                    e.Cancel = true;
                    string addresses = string.Join(", ", invalidRecipients.Select(r => r.Address));
                    e.Response = new SmtpResponse(550, $"Invalid recipients: {addresses}");
                }
            };

            return server;
        }

        /// <summary>
        /// Adds domain-based recipient validation
        /// </summary>
        public static ISmtpServer AddAllowedDomains(this ISmtpServer server,
            params string[] allowedDomains)
        {
            HashSet<string> domains = new(allowedDomains, StringComparer.OrdinalIgnoreCase);

            return server.AddRecipientValidation(recipient =>
                domains.Contains(recipient.Host));
        }

        /// <summary>
        /// Adds statistics tracking
        /// </summary>
        public static ISmtpServer AddStatistics(this ISmtpServer server, IStatisticsCollector collector)
        {
            ArgumentNullException.ThrowIfNull(server);

            ArgumentNullException.ThrowIfNull(collector);

            server.SessionCreated += (sender, e) => collector.RecordSession();
            server.MessageReceived += (sender, e) => collector.RecordMessage(e.Message);
            server.ErrorOccurred += (sender, e) => collector.RecordError(e.Exception);

            return server;
        }
    }
}