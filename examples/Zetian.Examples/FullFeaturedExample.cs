using Microsoft.Extensions.Logging;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Zetian.Abstractions;
using Zetian.Extensions;
using Zetian.Models;
using Zetian.RateLimiting;
using Zetian.Server;

namespace Zetian.Examples
{
    /// <summary>
    /// Full featured SMTP server with all capabilities
    /// </summary>
    public static class FullFeaturedExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("Starting Full Featured SMTP Server on port 587...");
            Console.WriteLine("Features: Authentication, TLS, Rate Limiting, Message Storage, Filtering");
            Console.WriteLine();

            // Setup
            string storageDir = Path.Combine(Directory.GetCurrentDirectory(), "full_smtp_messages");
            Directory.CreateDirectory(storageDir);

            X509Certificate2 certificate = CreateSelfSignedCertificate();
            InMemoryRateLimiter rateLimiter = new(RateLimitConfiguration.PerHour(100));

            // Setup logging
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Information)
                    .AddConsole();
            });

            // Create full featured SMTP server
            using SmtpServer server = new SmtpServerBuilder()
                .Port(587)
                .ServerName("Zetian Full Featured SMTP Server")
                .MaxMessageSizeMB(50)
                .MaxRecipients(50)
                .MaxConnections(100)
                .MaxConnectionsPerIP(100)

                // Security
                .Certificate(certificate)
                .RequireAuthentication()
                .AllowPlainTextAuthentication(true) // Allow plain text auth for testing, but TLS is available via STARTTLS
                .SslProtocols(SslProtocols.Tls12 | SslProtocols.Tls13)

                // Authentication
                .AddAuthenticationMechanism("PLAIN")
                .AddAuthenticationMechanism("LOGIN")
                .AuthenticationHandler(async (username, password) =>
                {
                    // In production, validate against database
                    Dictionary<string, string> validUsers = new()
                    {
                        { "admin", "admin123" },
                        { "user1", "pass123" },
                        { "demo", "demo123" }
                    };

                    if (validUsers.TryGetValue(username, out string? validPassword) && validPassword == password)
                    {
                        Console.WriteLine($"[AUTH] User {username} authenticated successfully");
                        return AuthenticationResult.Succeed(username);
                    }

                    Console.WriteLine($"[AUTH] Authentication failed for {username}");
                    return AuthenticationResult.Fail("Invalid credentials");
                })

                // Features
                .EnablePipelining()
                .Enable8BitMime()
                .ConnectionTimeout(TimeSpan.FromMinutes(5))
                .CommandTimeout(TimeSpan.FromMinutes(1))
                .DataTimeout(TimeSpan.FromMinutes(3))

                // Logging
                .LoggerFactory(loggerFactory)
                .EnableVerboseLogging(false)

                // Custom messages
                .Greeting("Welcome to Zetian SMTP Server")
                .Banner("Zetian SMTP Server v1.0")

                .Build();

            // Add extensions
            server
                .AddRateLimiting(rateLimiter)
                .SaveMessagesToDirectory(storageDir)
                .AddAllowedDomains("example.com", "test.com", "demo.com", "localhost")
                .AddSpamFilter(new[] { "spam.com", "junk.org", "phishing.net" })
                .AddSizeFilter(50 * 1024 * 1024); // 50MB max

            // Statistics tracking
            SimpleStatisticsCollector stats = new();
            server.AddStatistics(stats);

            // Custom message processing
            server.MessageReceived += async (sender, e) =>
            {
                Console.WriteLine($"\n[MESSAGE RECEIVED]");
                Console.WriteLine($"  Session ID: {e.Session.Id}");
                Console.WriteLine($"  Authenticated: {e.Session.IsAuthenticated}");
                if (e.Session.IsAuthenticated)
                {
                    Console.WriteLine($"  User: {e.Session.AuthenticatedIdentity}");
                }
                Console.WriteLine($"  Secure: {e.Session.IsSecure}");
                Console.WriteLine($"  Message ID: {e.Message.Id}");
                Console.WriteLine($"  From: {e.Message.From?.Address}");
                Console.WriteLine($"  To: {string.Join(", ", e.Message.Recipients)}");
                Console.WriteLine($"  Subject: {e.Message.Subject}");
                Console.WriteLine($"  Size: {e.Message.Size:N0} bytes");
                Console.WriteLine($"  Priority: {e.Message.Priority}");
                Console.WriteLine($"  Has Attachments: {e.Message.HasAttachments}");

                if (e.Message.Date.HasValue)
                {
                    Console.WriteLine($"  Date: {e.Message.Date:yyyy-MM-dd HH:mm:ss}");
                }

                // Simulate virus scanning
                Console.WriteLine("  [SCAN] Scanning for viruses...");
                await Task.Delay(100);
                Console.WriteLine("  [SCAN] Message is clean");

                // Simulate spam scoring
                int spamScore = new Random().Next(0, 10);
                Console.WriteLine($"  [SPAM] Spam score: {spamScore}/10");

                if (spamScore > 7)
                {
                    Console.WriteLine("  [SPAM] Message marked as potential spam");
                    e.Session.Properties["SpamScore"] = spamScore;
                }

                Console.WriteLine();
            };

            // Session events
            server.SessionCreated += (sender, e) =>
            {
                Console.WriteLine($"[SESSION START] Connection from {e.Session.RemoteEndPoint}");
            };

            server.SessionCompleted += (sender, e) =>
            {
                Console.WriteLine($"[SESSION END] {e.Session.RemoteEndPoint} - Duration: {(DateTime.UtcNow - e.Session.StartTime).TotalSeconds:F1}s, Messages: {e.Session.MessageCount}");
            };

            server.ErrorOccurred += (sender, e) =>
            {
                Console.WriteLine($"[ERROR] {e.Exception.GetType().Name}: {e.Exception.Message}");
            };

            // Start server
            CancellationTokenSource cts = new();
            await server.StartAsync(cts.Token);

            Console.WriteLine($"Server is running on {server.Endpoint}");
            Console.WriteLine();
            Console.WriteLine("Configuration:");
            Console.WriteLine($"  Storage: {storageDir}");
            Console.WriteLine("  Authentication required: Yes");
            Console.WriteLine("  TLS/SSL: Available (STARTTLS)");
            Console.WriteLine("  Rate limit: 100 messages/hour per IP");
            Console.WriteLine("  Allowed domains: example.com, test.com, demo.com, localhost");
            Console.WriteLine("  Max message size: 50MB");
            Console.WriteLine();
            Console.WriteLine("Test Credentials:");
            Console.WriteLine("  admin / admin123");
            Console.WriteLine("  user1 / pass123");
            Console.WriteLine("  demo / demo123");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  S - Show statistics");
            Console.WriteLine("  Q - Quit");
            Console.WriteLine();

            // Handle commands
            bool running = true;
            while (running)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.S:
                        ShowStatistics(stats);
                        break;
                    case ConsoleKey.Q:
                        running = false;
                        break;
                }
            }

            await server.StopAsync();
            rateLimiter.Dispose();
            loggerFactory.Dispose();

            Console.WriteLine("Server stopped.");
            ShowStatistics(stats);
        }

        private static X509Certificate2 CreateSelfSignedCertificate()
        {
            X500DistinguishedName distinguishedName = new("CN=smtp.zetian.local");
            using RSA rsa = RSA.Create(2048);

            CertificateRequest request = new(
                distinguishedName,
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

            X509Certificate2 certificate = request.CreateSelfSigned(
                DateTimeOffset.Now,
                DateTimeOffset.Now.AddYears(1));

            return new X509Certificate2(
                certificate.Export(X509ContentType.Pfx, ""),
                "",
                X509KeyStorageFlags.MachineKeySet);
        }

        private static void ShowStatistics(SimpleStatisticsCollector stats)
        {
            Console.WriteLine("\n=== Server Statistics ===");
            Console.WriteLine($"  Total Sessions: {stats.TotalSessions:N0}");
            Console.WriteLine($"  Total Messages: {stats.TotalMessages:N0}");
            Console.WriteLine($"  Total Errors: {stats.TotalErrors:N0}");
            Console.WriteLine($"  Total Data: {FormatBytes(stats.TotalBytes)}");
            Console.WriteLine($"  Average Message Size: {(stats.TotalMessages > 0 ? FormatBytes(stats.TotalBytes / stats.TotalMessages) : "N/A")}");
            Console.WriteLine("========================\n");
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }

    public class SimpleStatisticsCollector : IStatisticsCollector
    {
        private long _totalSessions;
        private long _totalMessages;
        private long _totalErrors;
        private long _totalBytes;

        public long TotalSessions => _totalSessions;
        public long TotalMessages => _totalMessages;
        public long TotalErrors => _totalErrors;
        public long TotalBytes => _totalBytes;

        public void RecordSession()
        {
            Interlocked.Increment(ref _totalSessions);
        }

        public void RecordMessage(ISmtpMessage message)
        {
            Interlocked.Increment(ref _totalMessages);
            Interlocked.Add(ref _totalBytes, message.Size);
        }

        public void RecordError(Exception exception)
        {
            Interlocked.Increment(ref _totalErrors);
        }
    }
}