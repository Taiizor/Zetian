using Microsoft.Extensions.Logging;
using Zetian.Configuration;
using Zetian.Protocol;
using Zetian.Server;

namespace Zetian.Examples
{
    /// <summary>
    /// Example demonstrating comprehensive event handling in Zetian SMTP Server
    /// </summary>
    public class ComprehensiveEventsExample
    {
        public static async Task RunAsync()
        {
            // Create logger factory
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddConsole();
            });

            ILogger<ComprehensiveEventsExample> logger = loggerFactory.CreateLogger<ComprehensiveEventsExample>();

            try
            {
                logger.LogInformation("Starting SMTP server with comprehensive event handling");

                // Create SMTP server with configuration
                SmtpServerConfiguration configuration = new()
                {
                    Port = 25,
                    ServerName = "Events Example SMTP",
                    MaxConnections = 100,
                    MaxConnectionsPerIp = 10,
                    MaxRecipients = 100,
                    MaxMessageSize = 10485760, // 10 MB
                    RequireAuthentication = false,
                    EnableVerboseLogging = true,
                    LoggerFactory = loggerFactory
                };

                SmtpServer server = new(configuration);

                // Wire up all events
                SetupConnectionEvents(server, logger);
                SetupSessionEvents(server, logger);
                SetupCommandEvents(server, logger);
                SetupAuthenticationEvents(server, logger);
                SetupTlsEvents(server, logger);
                SetupDataTransferEvents(server, logger);
                SetupMessageEvents(server, logger);
                SetupRateLimitEvents(server, logger);
                SetupErrorEvents(server, logger);

                // Start the server
                await server.StartAsync();

                Console.WriteLine("==========================================");
                Console.WriteLine("SMTP Server with Comprehensive Events");
                Console.WriteLine("==========================================");
                Console.WriteLine($"Server: {configuration.ServerName}");
                Console.WriteLine($"Port: {configuration.Port}");
                Console.WriteLine($"Max Connections: {configuration.MaxConnections}");
                Console.WriteLine($"Max Connections Per IP: {configuration.MaxConnectionsPerIp}");
                Console.WriteLine();
                Console.WriteLine("All events are being logged to console");
                Console.WriteLine("Press 'Q' to quit");
                Console.WriteLine("==========================================");

                // Wait for quit
                while (true)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Q)
                    {
                        break;
                    }
                }

                logger.LogInformation("Shutting down server");
                await server.StopAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Server error");
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("Server stopped. Press any key to exit...");
            Console.ReadKey();
        }

        private static void SetupConnectionEvents(SmtpServer server, ILogger logger)
        {
            server.ConnectionAccepted += (sender, e) =>
            {
                logger.LogInformation("✅ Connection ACCEPTED from {RemoteEndPoint} to {LocalEndPoint}",
                    e.RemoteEndPoint, e.LocalEndPoint);
            };

            server.ConnectionRejected += (sender, e) =>
            {
                logger.LogWarning("❌ Connection REJECTED from {RemoteEndPoint} - Reason: {Reason}",
                    e.RemoteEndPoint, e.RejectionReason);
            };
        }

        private static void SetupSessionEvents(SmtpServer server, ILogger logger)
        {
            server.SessionCreated += (sender, e) =>
            {
                logger.LogInformation("📝 Session CREATED: {SessionId} from {RemoteEndPoint}",
                    e.Session.Id, e.Session.RemoteEndPoint);
            };

            server.SessionCompleted += (sender, e) =>
            {
                TimeSpan duration = DateTime.UtcNow - e.Session.StartTime;
                logger.LogInformation("✔️ Session COMPLETED: {SessionId} - Duration: {Duration:F2}s, Messages: {MessageCount}",
                    e.Session.Id, duration.TotalSeconds, e.Session.MessageCount);
            };
        }

        private static void SetupCommandEvents(SmtpServer server, ILogger logger)
        {
            server.CommandReceived += (sender, e) =>
            {
                logger.LogDebug("⬇️ Command RECEIVED: {Command} - Raw: {RawCommand}",
                    e.Command.Verb, e.RawCommand);

                // Example: Block certain commands
                if (e.Command.Verb == "VRFY")
                {
                    e.Cancel = true;
                    e.Response = new SmtpResponse(502, "VRFY command disabled");
                    logger.LogWarning("🚫 Blocked VRFY command from session {SessionId}", e.Session.Id);
                }
            };

            server.CommandExecuted += (sender, e) =>
            {
                logger.LogDebug("⬆️ Command EXECUTED: {Command} - Success: {Success}, Duration: {Duration:F2}ms",
                    e.Command.Verb, e.Success, e.DurationMs ?? 0);
            };
        }

        private static void SetupAuthenticationEvents(SmtpServer server, ILogger logger)
        {
            server.AuthenticationAttempted += (sender, e) =>
            {
                logger.LogInformation("🔐 Authentication ATTEMPTED: Mechanism: {Mechanism}, Session: {SessionId}",
                    e.Mechanism, e.Session.Id);
            };

            server.AuthenticationSucceeded += (sender, e) =>
            {
                logger.LogInformation("✅ Authentication SUCCEEDED: Identity: {Identity}, Session: {SessionId}",
                    e.AuthenticatedIdentity, e.Session.Id);
            };

            server.AuthenticationFailed += (sender, e) =>
            {
                logger.LogWarning("❌ Authentication FAILED: Mechanism: {Mechanism}, Session: {SessionId}",
                    e.Mechanism, e.Session.Id);
            };
        }

        private static void SetupTlsEvents(SmtpServer server, ILogger logger)
        {
            server.TlsNegotiationStarted += (sender, e) =>
            {
                logger.LogInformation("🔒 TLS negotiation STARTED: Session: {SessionId}",
                    e.Session.Id);
            };

            server.TlsNegotiationCompleted += (sender, e) =>
            {
                logger.LogInformation("✅ TLS negotiation COMPLETED: Protocol: {Protocol}, Cipher: {Cipher}, Session: {SessionId}",
                    e.ProtocolVersion, e.CipherSuite, e.Session.Id);
            };

            server.TlsNegotiationFailed += (sender, e) =>
            {
                logger.LogError("❌ TLS negotiation FAILED: Error: {Error}, Session: {SessionId}",
                    e.ErrorMessage, e.Session.Id);
            };
        }

        private static void SetupDataTransferEvents(SmtpServer server, ILogger logger)
        {
            server.DataTransferStarted += (sender, e) =>
            {
                logger.LogInformation("📥 Data transfer STARTED: From: {From}, Recipients: {Recipients}, Session: {SessionId}",
                    e.From, string.Join(", ", e.Recipients), e.Session.Id);

                // Example: Reject large messages early
                if (e.Recipients.Count > 50)
                {
                    e.Cancel = true;
                    logger.LogWarning("🚫 Rejected data transfer - too many recipients ({Count})", e.Recipients.Count);
                }
            };

            server.DataTransferCompleted += (sender, e) =>
            {
                if (e.Success)
                {
                    double throughput = e.BytesTransferred / (e.DurationMs / 1000.0) / 1024.0;
                    logger.LogInformation("✅ Data transfer COMPLETED: {Bytes} bytes in {Duration:F2}ms ({Throughput:F2} KB/s), Session: {SessionId}",
                        e.BytesTransferred, e.DurationMs, throughput, e.Session.Id);
                }
                else
                {
                    logger.LogWarning("❌ Data transfer FAILED: Error: {Error}, Session: {SessionId}",
                        e.ErrorMessage, e.Session.Id);
                }
            };
        }

        private static void SetupMessageEvents(SmtpServer server, ILogger logger)
        {
            server.MessageReceived += (sender, e) =>
            {
                logger.LogInformation("📧 Message RECEIVED: Id: {MessageId}, From: {From}, To: {To}, Size: {Size} bytes",
                    e.Message.Id, e.Message.From, string.Join(", ", e.Message.Recipients), e.Message.Size);

                // Example: Simple spam filter
                if (e.Message.From?.Address?.Contains("spam") == true)
                {
                    e.Cancel = true;
                    e.Response = new SmtpResponse(550, "Message rejected: spam detected");
                    logger.LogWarning("🚫 Rejected spam message from {From}", e.Message.From);
                }
            };
        }

        private static void SetupRateLimitEvents(SmtpServer server, ILogger logger)
        {
            server.RateLimitExceeded += (sender, e) =>
            {
                logger.LogWarning("⚠️ Rate limit EXCEEDED: IP: {IP}, Current: {Current}/{Limit}, Window: {Window}, Reset: {Reset}",
                    e.IpAddress, e.CurrentCount, e.Limit, e.TimeWindow, e.ResetTime);

                // Custom response message
                e.ResponseMessage = $"Rate limit exceeded. Please try again after {e.ResetTime:HH:mm:ss}";
            };
        }

        private static void SetupErrorEvents(SmtpServer server, ILogger logger)
        {
            server.ErrorOccurred += (sender, e) =>
            {
                if (e.Session != null)
                {
                    logger.LogError(e.Exception, "❌ ERROR in session {SessionId}: {Message}",
                        e.Session.Id, e.Exception.Message);
                }
                else
                {
                    logger.LogError(e.Exception, "❌ Server ERROR: {Message}",
                        e.Exception.Message);
                }
            };
        }
    }
}