using Zetian.Abstractions;
using Zetian.Extensions;
using Zetian.Server;

namespace Zetian.Examples
{
    /// <summary>
    /// SMTP server with custom message processing
    /// </summary>
    public static class CustomProcessingExample
    {
        private static readonly Dictionary<string, List<ISmtpMessage>> _mailboxes = new();
        private static readonly object _lock = new();

        public static async Task RunAsync()
        {
            Console.WriteLine("Starting SMTP Server with Custom Processing on port 25...");
            Console.WriteLine("This server stores messages in memory mailboxes.");
            Console.WriteLine();

            // Create SMTP server
            using SmtpServer server = new SmtpServerBuilder()
                .Port(25)
                .ServerName("Custom Processing SMTP Server")
                .MaxMessageSizeMB(25)
                .Build();

            // Add custom message processing
            server.MessageReceived += (sender, e) =>
            {
                ProcessMessage(e.Message);
                DisplayMailboxStatus();
            };

            // Add custom recipient validation
            server.AddRecipientValidation(recipient =>
            {
                // Only accept emails for specific users
                string[] validUsers = new[] { "admin", "user", "test", "info", "support" };
                string localPart = recipient.User.ToLower();

                if (validUsers.Contains(localPart))
                {
                    Console.WriteLine($"[VALIDATION] Accepted recipient: {recipient.Address}");
                    return true;
                }

                Console.WriteLine($"[VALIDATION] Rejected recipient: {recipient.Address}");
                return false;
            });

            // Add custom message filter
            server.AddMessageFilter(message =>
            {
                // Block messages with certain keywords in subject
                string[] blockedWords = new[] { "viagra", "casino", "lottery" };
                string subject = message.Subject?.ToLower() ?? "";

                foreach (string? word in blockedWords)
                {
                    if (subject.Contains(word))
                    {
                        Console.WriteLine($"[FILTER] Blocked message with subject containing '{word}'");
                        return false;
                    }
                }

                return true;
            });

            // Add message forwarding to external service
            server.ForwardMessages(async message =>
            {
                // Simulate forwarding to external service
                Console.WriteLine($"[FORWARD] Forwarding message {message.Id} to external service...");
                await Task.Delay(100); // Simulate network call
                Console.WriteLine($"[FORWARD] Message {message.Id} forwarded successfully");
                return true;
            });

            // Start the server
            CancellationTokenSource cts = new();
            await server.StartAsync(cts.Token);

            Console.WriteLine($"Server is running on {server.Endpoint}");
            Console.WriteLine("Valid recipients: admin@*, user@*, test@*, info@*, support@*");
            Console.WriteLine("Blocked subjects containing: viagra, casino, lottery");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  L - List mailboxes");
            Console.WriteLine("  C - Clear all mailboxes");
            Console.WriteLine("  Q - Quit");
            Console.WriteLine();

            // Handle user commands
            bool running = true;
            while (running)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.L:
                        DisplayDetailedMailboxStatus();
                        break;
                    case ConsoleKey.C:
                        ClearMailboxes();
                        break;
                    case ConsoleKey.Q:
                        running = false;
                        break;
                }
            }

            await server.StopAsync();
            Console.WriteLine("Server stopped.");
        }

        private static void ProcessMessage(ISmtpMessage message)
        {
            lock (_lock)
            {
                foreach (System.Net.Mail.MailAddress recipient in message.Recipients)
                {
                    string mailbox = recipient.User.ToLower();

                    if (!_mailboxes.ContainsKey(mailbox))
                    {
                        _mailboxes[mailbox] = new List<ISmtpMessage>();
                    }

                    _mailboxes[mailbox].Add(message);
                    Console.WriteLine($"[MAILBOX] Message stored in mailbox: {mailbox}");
                }
            }
        }

        private static void DisplayMailboxStatus()
        {
            lock (_lock)
            {
                Console.WriteLine($"[STATUS] Total mailboxes: {_mailboxes.Count}");
                Console.WriteLine($"[STATUS] Total messages: {_mailboxes.Sum(m => m.Value.Count)}");
                Console.WriteLine();
            }
        }

        private static void DisplayDetailedMailboxStatus()
        {
            lock (_lock)
            {
                Console.WriteLine("\n=== Mailbox Status ===");

                if (_mailboxes.Count == 0)
                {
                    Console.WriteLine("No mailboxes with messages.");
                }
                else
                {
                    foreach (KeyValuePair<string, List<ISmtpMessage>> mailbox in _mailboxes)
                    {
                        Console.WriteLine($"\nMailbox: {mailbox.Key}");
                        Console.WriteLine($"  Messages: {mailbox.Value.Count}");

                        foreach (ISmtpMessage? msg in mailbox.Value.Take(5)) // Show last 5 messages
                        {
                            Console.WriteLine($"    - From: {msg.From?.Address}, Subject: {msg.Subject}");
                        }

                        if (mailbox.Value.Count > 5)
                        {
                            Console.WriteLine($"    ... and {mailbox.Value.Count - 5} more messages");
                        }
                    }
                }

                Console.WriteLine("\n===================\n");
            }
        }

        private static void ClearMailboxes()
        {
            lock (_lock)
            {
                int count = _mailboxes.Sum(m => m.Value.Count);
                _mailboxes.Clear();
                Console.WriteLine($"\n[CLEAR] Cleared {count} messages from all mailboxes.\n");
            }
        }
    }
}