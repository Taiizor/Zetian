using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using Zetian.Abstractions;
using Zetian.Relay.Configuration;
using Zetian.Relay.Extensions;
using Zetian.Server;

namespace Zetian.Relay.Examples
{
    /// <summary>
    /// Demonstrates authentication and relay access control
    /// </summary>
    public static class AuthenticationExample
    {
        public static async Task RunAsync(ILoggerFactory loggerFactory)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("       Authentication Example");
            Console.WriteLine("===========================================");
            Console.WriteLine();
            Console.WriteLine("This example demonstrates:");
            Console.WriteLine("- Authentication requirements for relay");
            Console.WriteLine("- Relay networks (IPs allowed without auth)");
            Console.WriteLine("- AUTH PLAIN and LOGIN mechanisms");
            Console.WriteLine("- Access control based on authentication");
            Console.WriteLine();

            // Create server with authentication requirements
            ISmtpServer server = SmtpServerBuilder
                .CreateBasic()
                .Port(25033)
                .ServerName("auth-relay.local")
                .EnableAuthentication(AuthenticationMechanism.Plain | AuthenticationMechanism.Login)
                .AddUser("relay_user", "relay_pass123")
                .AddUser("admin", "admin_secret")
                .AddUser("sender", "sender_password")
                .LoggerFactory(loggerFactory)
                .EnableRelay(config =>
                {
                    // Require authentication for relay
                    config.RequireAuthentication = true;

                    // Allow relay from localhost without authentication
                    config.RelayNetworks.Add(IPAddress.Loopback);
                    config.RelayNetworks.Add(IPAddress.IPv6Loopback);
                    config.RelayNetworks.Add(IPAddress.Parse("192.168.1.0"));

                    // Configure smart host with authentication
                    config.DefaultSmartHost = new SmartHostConfiguration
                    {
                        Host = "authenticated.smtp.provider.com",
                        Port = 587,
                        UseStartTls = true,
                        Credentials = new NetworkCredential("provider_username", "provider_password")
                    };

                    // Set local domains (no auth required for local delivery)
                    config.LocalDomains.Add("auth-relay.local");
                    config.LocalDomains.Add("internal.local");
                });

            // Log authentication and relay events
            server.SessionStarted += (sender, e) =>
            {
                Console.WriteLine($"[SESSION] New connection from {e.Session.RemoteEndPoint}");
            };

            server.SessionAuthenticated += (sender, e) =>
            {
                Console.WriteLine($"[AUTH] User '{e.Username}' authenticated successfully");
                Console.WriteLine($"  → Relay access: GRANTED");
            };

            server.MessageReceived += async (sender, e) =>
            {
                bool isLocal = e.Message.Recipients.All(r =>
                    r.Host.Equals("auth-relay.local", StringComparison.OrdinalIgnoreCase) ||
                    r.Host.Equals("internal.local", StringComparison.OrdinalIgnoreCase));

                Console.WriteLine($"\n[MESSAGE] From: {e.Message.From?.Address}");
                Console.WriteLine($"  To: {string.Join(", ", e.Message.Recipients)}");
                Console.WriteLine($"  Authenticated: {(e.Session.IsAuthenticated ? "Yes" : "No")}");

                if (e.Session.IsAuthenticated)
                {
                    Console.WriteLine($"  User: {e.Session.AuthenticatedUser}");
                    Console.WriteLine($"  Action: {(isLocal ? "LOCAL DELIVERY" : "RELAY")}");
                }
                else
                {
                    var ipEndPoint = e.Session.RemoteEndPoint as IPEndPoint;
                    bool isRelayNetwork = ipEndPoint != null &&
                        (ipEndPoint.Address.Equals(IPAddress.Loopback) ||
                         ipEndPoint.Address.Equals(IPAddress.IPv6Loopback) ||
                         ipEndPoint.Address.ToString().StartsWith("192.168.1."));

                    if (isRelayNetwork)
                    {
                        Console.WriteLine($"  Relay Network: {ipEndPoint?.Address}");
                        Console.WriteLine($"  Action: {(isLocal ? "LOCAL DELIVERY" : "RELAY (Network Authorized)")}");
                    }
                    else if (isLocal)
                    {
                        Console.WriteLine($"  Action: LOCAL DELIVERY (No auth required)");
                    }
                    else
                    {
                        Console.WriteLine($"  Action: RELAY DENIED (Authentication required)");
                    }
                }

                await Task.CompletedTask;
            };

            // Start server
            Console.WriteLine("[INFO] Starting SMTP server with authentication...");
            await server.StartWithRelayAsync();
            Console.WriteLine("[INFO] Server started on port 25033");
            Console.WriteLine();

            // Display configuration
            Console.WriteLine("[CONFIG] Authentication Configuration:");
            Console.WriteLine("  Authentication Required: YES (for relay)");
            Console.WriteLine("  Mechanisms: PLAIN, LOGIN");
            Console.WriteLine("  Users:");
            Console.WriteLine("    - relay_user / relay_pass123");
            Console.WriteLine("    - admin / admin_secret");
            Console.WriteLine("    - sender / sender_password");
            Console.WriteLine();
            Console.WriteLine("  Relay Networks (no auth required):");
            Console.WriteLine("    - 127.0.0.1 (localhost)");
            Console.WriteLine("    - ::1 (IPv6 localhost)");
            Console.WriteLine("    - 192.168.1.0/24");
            Console.WriteLine();
            Console.WriteLine("  Local Domains (no relay):");
            Console.WriteLine("    - auth-relay.local");
            Console.WriteLine("    - internal.local");
            Console.WriteLine();

            // Test scenarios
            Console.WriteLine("[TEST] Testing various authentication scenarios...");
            Console.WriteLine();

            // Test 1: No authentication, local delivery
            Console.WriteLine("Test 1: Local delivery without authentication");
            await TestScenario(
                "localhost", 25033,
                null, null,
                "unauthenticated@example.com",
                "local-user@auth-relay.local",
                "Local delivery test"
            );

            await Task.Delay(1000);

            // Test 2: No authentication, relay attempt (should fail)
            Console.WriteLine("\nTest 2: Relay attempt without authentication (should fail)");
            await TestScenario(
                "localhost", 25033,
                null, null,
                "unauthenticated@example.com",
                "external@gmail.com",
                "Unauthorized relay test"
            );

            await Task.Delay(1000);

            // Test 3: With authentication, relay allowed
            Console.WriteLine("\nTest 3: Relay with valid authentication");
            await TestScenario(
                "localhost", 25033,
                "relay_user", "relay_pass123",
                "authenticated@example.com",
                "external@gmail.com",
                "Authenticated relay test"
            );

            await Task.Delay(1000);

            // Test 4: Wrong credentials
            Console.WriteLine("\nTest 4: Invalid credentials (should fail)");
            await TestScenario(
                "localhost", 25033,
                "relay_user", "wrong_password",
                "hacker@example.com",
                "victim@gmail.com",
                "Failed auth test"
            );

            await Task.Delay(1000);

            // Test 5: Mixed recipients (local and external) with auth
            Console.WriteLine("\nTest 5: Mixed recipients with authentication");
            await TestMixedRecipients();

            // Interactive testing
            Console.WriteLine();
            Console.WriteLine("[INTERACTIVE] You can now test authentication manually");
            Console.WriteLine("Server: localhost:25033");
            Console.WriteLine("Users: relay_user/relay_pass123, admin/admin_secret");
            Console.WriteLine();
            Console.WriteLine("Press 'Q' to quit");

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Q)
                {
                    break;
                }
            }

            // Stop server
            Console.WriteLine();
            Console.WriteLine("[INFO] Stopping server...");
            await server.StopAsync();
            Console.WriteLine("[INFO] Server stopped.");
        }

        private static async Task TestScenario(
            string host, int port,
            string? username, string? password,
            string from, string to, string subject)
        {
            try
            {
                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = false,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                if (!string.IsNullOrEmpty(username))
                {
                    client.Credentials = new NetworkCredential(username, password);
                }

                var message = new MailMessage
                {
                    From = new MailAddress(from),
                    Subject = subject,
                    Body = $"Test message sent at {DateTime.Now:HH:mm:ss}",
                    IsBodyHtml = false
                };
                message.To.Add(to);

                await client.SendMailAsync(message);
                Console.WriteLine($"  ✓ SUCCESS: Message sent");
            }
            catch (SmtpException ex)
            {
                Console.WriteLine($"  ✗ EXPECTED FAILURE: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ ERROR: {ex.Message}");
            }
        }

        private static async Task TestMixedRecipients()
        {
            try
            {
                using var client = new SmtpClient("localhost", 25033)
                {
                    EnableSsl = false,
                    Credentials = new NetworkCredential("admin", "admin_secret")
                };

                var message = new MailMessage
                {
                    From = new MailAddress("admin@auth-relay.local"),
                    Subject = "Mixed Recipients Test",
                    Body = "Testing mixed local and external recipients",
                    IsBodyHtml = false
                };

                // Add both local and external recipients
                message.To.Add("user1@auth-relay.local");  // Local
                message.To.Add("user2@internal.local");    // Local
                message.To.Add("user@gmail.com");          // External (relay)
                message.To.Add("contact@yahoo.com");       // External (relay)

                await client.SendMailAsync(message);
                Console.WriteLine($"  ✓ SUCCESS: Mixed recipients handled correctly");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ ERROR: {ex.Message}");
            }
        }
    }
}
