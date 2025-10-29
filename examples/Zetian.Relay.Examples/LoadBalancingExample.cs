using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Mail;
using Zetian.Abstractions;
using Zetian.Relay.Configuration;
using Zetian.Relay.Extensions;
using Zetian.Relay.Models;
using Zetian.Relay.Services;
using Zetian.Server;

namespace Zetian.Relay.Examples
{
    /// <summary>
    /// Demonstrates load balancing across multiple smart hosts
    /// </summary>
    public static class LoadBalancingExample
    {
        public static async Task RunAsync(ILoggerFactory loggerFactory)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("       Load Balancing Example");
            Console.WriteLine("===========================================");
            Console.WriteLine();
            Console.WriteLine("This example demonstrates:");
            Console.WriteLine("- Weight-based load distribution");
            Console.WriteLine("- Multiple smart hosts with same priority");
            Console.WriteLine("- Connection pooling and limits");
            Console.WriteLine("- Traffic distribution monitoring");
            Console.WriteLine();

            // Create server with load-balanced smart hosts
            ISmtpServer server = new SmtpServerBuilder()
                .Port(25032)
                .ServerName("loadbalancer.local")
                .LoggerFactory(loggerFactory)
                .Build()
                .EnableRelay(config =>
                {
                    // Configure multiple smart hosts with same priority but different weights
                    // Total weight: 100 (for easy percentage calculation)

                    // Server 1: 40% of traffic
                    config.SmartHosts.Add(new SmartHostConfiguration
                    {
                        Host = "smtp1.loadbalance.example.com",
                        Port = 25,
                        Priority = 10, // Same priority for load balancing
                        Weight = 40,   // 40% of traffic
                        MaxConnections = 10,
                        MaxMessagesPerConnection = 50,
                        Enabled = true
                    });

                    // Server 2: 30% of traffic
                    config.SmartHosts.Add(new SmartHostConfiguration
                    {
                        Host = "smtp2.loadbalance.example.com",
                        Port = 25,
                        Priority = 10, // Same priority
                        Weight = 30,   // 30% of traffic
                        MaxConnections = 8,
                        MaxMessagesPerConnection = 50,
                        Enabled = true
                    });

                    // Server 3: 20% of traffic
                    config.SmartHosts.Add(new SmartHostConfiguration
                    {
                        Host = "smtp3.loadbalance.example.com",
                        Port = 25,
                        Priority = 10, // Same priority
                        Weight = 20,   // 20% of traffic
                        MaxConnections = 5,
                        MaxMessagesPerConnection = 50,
                        Enabled = true
                    });

                    // Server 4: 10% of traffic (backup/low capacity)
                    config.SmartHosts.Add(new SmartHostConfiguration
                    {
                        Host = "smtp4.loadbalance.example.com",
                        Port = 25,
                        Priority = 10, // Same priority
                        Weight = 10,   // 10% of traffic
                        MaxConnections = 3,
                        MaxMessagesPerConnection = 25,
                        Enabled = true
                    });

                    config.MaxConcurrentDeliveries = 20;
                    config.ConnectionTimeout = TimeSpan.FromSeconds(10);

                    // Allow relay without authentication for demo
                    config.RequireAuthentication = false;
                });

            // Track load distribution (simulated for demo purposes)
            ConcurrentDictionary<string, int> serverStats = new();

            server.MessageReceived += (sender, e) =>
            {
                // Note: This is just simulation for demo visualization
                // Actual load balancing happens in the relay service
                string selectedServer = SimulateWeightedSelection();
                serverStats.AddOrUpdate(selectedServer, 1, (key, oldValue) => oldValue + 1);

                Console.WriteLine($"[LOAD-BALANCE] Message from {e.Message.From?.Address}");
                Console.WriteLine($"  → Assigned to: {selectedServer}");
            };

            // Start server
            Console.WriteLine("[INFO] Starting SMTP server with load balancing...");
            RelayService relayService = await server.StartWithRelayAsync();
            Console.WriteLine("[INFO] Server started on port 25032");
            Console.WriteLine();

            // Display load balancing configuration
            Console.WriteLine("[CONFIG] Load Balancing Configuration:");
            Console.WriteLine("┌─────────────────────────────────┬──────────┬────────┬────────────┬──────────┐");
            Console.WriteLine("│ Smart Host                      │ Priority │ Weight │ Max Conn   │ Capacity │");
            Console.WriteLine("├─────────────────────────────────┼──────────┼────────┼────────────┼──────────┤");
            Console.WriteLine("│ smtp1.loadbalance.example.com   │    10    │  40%   │     10     │   High   │");
            Console.WriteLine("│ smtp2.loadbalance.example.com   │    10    │  30%   │      8     │   High   │");
            Console.WriteLine("│ smtp3.loadbalance.example.com   │    10    │  20%   │      5     │  Medium  │");
            Console.WriteLine("│ smtp4.loadbalance.example.com   │    10    │  10%   │      3     │   Low    │");
            Console.WriteLine("└─────────────────────────────────┴──────────┴────────┴────────────┴──────────┘");
            Console.WriteLine();

            // Send test messages to see load distribution
            Console.WriteLine("[TEST] Sending 100 test messages to observe load distribution...");
            Console.WriteLine();

            using SmtpClient client = new("localhost", 25032)
            {
                EnableSsl = false
            };

            DateTime startTime = DateTime.Now;
            for (int i = 1; i <= 100; i++)
            {
                MailMessage message = new()
                {
                    From = new MailAddress($"sender{i}@example.com"),
                    Subject = $"Load Balance Test #{i}",
                    Body = $"Test message {i} for load balancing demonstration - {DateTime.Now:HH:mm:ss.fff}",
                    IsBodyHtml = false
                };
                message.To.Add($"recipient{i}@test.com");

                try
                {
                    await client.SendMailAsync(message);

                    // Show progress
                    if (i % 10 == 0)
                    {
                        Console.WriteLine($"  Progress: {i}/100 messages sent");
                        ShowDistribution(serverStats);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Failed message #{i}: {ex.Message}");
                }

                // Small delay to prevent overwhelming
                if (i % 5 == 0)
                {
                    await Task.Delay(100);
                }
            }

            TimeSpan duration = DateTime.Now - startTime;
            Console.WriteLine();
            Console.WriteLine($"[COMPLETE] Sent 100 messages in {duration.TotalSeconds:F2} seconds");
            Console.WriteLine($"  Average: {100 / duration.TotalSeconds:F2} messages/second");
            Console.WriteLine();

            // Show final distribution
            Console.WriteLine("[RESULTS] Final Load Distribution:");
            Console.WriteLine("┌─────────────────────────────────┬──────────┬────────────┬───────────┐");
            Console.WriteLine("│ Smart Host                      │ Expected │   Actual   │ Deviation │");
            Console.WriteLine("├─────────────────────────────────┼──────────┼────────────┼───────────┤");

            (string, int)[] servers = new[]
            {
                ("smtp1.loadbalance.example.com", 40),
                ("smtp2.loadbalance.example.com", 30),
                ("smtp3.loadbalance.example.com", 20),
                ("smtp4.loadbalance.example.com", 10)
            };

            foreach ((string? serverName, int expectedPercent) in servers)
            {
                int actual = serverStats.GetValueOrDefault(serverName, 0);
                int actualPercent = actual;
                int deviation = actualPercent - expectedPercent;
                string deviationStr = deviation >= 0 ? $"+{deviation}%" : $"{deviation}%";

                Console.WriteLine($"│ {serverName,-31} │   {expectedPercent,2}%    │    {actualPercent,2}%     │  {deviationStr,7}  │");
            }

            Console.WriteLine("└─────────────────────────────────┴──────────┴────────────┴───────────┘");
            Console.WriteLine();
            Console.WriteLine("[NOTE] Small deviations from expected percentages are normal");
            Console.WriteLine("       due to the random nature of weighted selection.");
            Console.WriteLine();

            // Monitor real-time load
            Console.WriteLine("[MONITOR] Press 'S' for current statistics, 'Q' to quit");

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Q)
                {
                    break;
                }

                if (key.Key == ConsoleKey.S)
                {
                    // Get real statistics from relay service
                    if (relayService != null && relayService.Queue != null)
                    {
                        RelayQueueStatistics stats = await relayService.Queue.GetStatisticsAsync();
                        Console.WriteLine($"\n[STATISTICS] {DateTime.Now:HH:mm:ss}");
                        Console.WriteLine($"  Total Messages: {stats.TotalMessages}");
                        Console.WriteLine($"  Active Deliveries: {stats.InProgressMessages}");
                        Console.WriteLine($"  Queued: {stats.QueuedMessages}");
                        Console.WriteLine($"  Delivered: {stats.DeliveredMessages}");
                        Console.WriteLine($"  Failed: {stats.FailedMessages}");
                        Console.WriteLine($"  Deferred: {stats.DeferredMessages}");

                        if (stats.TotalMessages == 0)
                        {
                            Console.WriteLine("\n[INFO] No messages in relay queue");
                            Console.WriteLine("  Messages may have been delivered locally or failed immediately");
                        }
                        else if (stats.MessagesBySmartHost?.Count > 0)
                        {
                            Console.WriteLine("\n  Distribution by Smart Host:");
                            foreach (KeyValuePair<string, int> kvp in stats.MessagesBySmartHost)
                            {
                                double percent = kvp.Value * 100.0 / stats.TotalMessages;
                                Console.WriteLine($"    {kvp.Key}: {kvp.Value} messages ({percent:F1}%)");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("[ERROR] Relay service not available");
                    }
                }
            }

            // Stop server
            Console.WriteLine();
            Console.WriteLine("[INFO] Stopping server...");
            await server.StopAsync();
            Console.WriteLine("[INFO] Server stopped.");
        }

        private static string SimulateWeightedSelection()
        {
            // Simulate weighted random selection
            int random = Random.Shared.Next(100);

            if (random < 40)
            {
                return "smtp1.loadbalance.example.com";
            }
            else if (random < 70)
            {
                return "smtp2.loadbalance.example.com";
            }
            else if (random < 90)
            {
                return "smtp3.loadbalance.example.com";
            }
            else
            {
                return "smtp4.loadbalance.example.com";
            }
        }

        private static void ShowDistribution(System.Collections.Concurrent.ConcurrentDictionary<string, int> stats)
        {
            int total = stats.Values.Sum();
            if (total == 0)
            {
                return;
            }

            Console.WriteLine("    Current distribution:");
            foreach (KeyValuePair<string, int> kvp in stats.OrderBy(x => x.Key))
            {
                double percent = kvp.Value * 100.0 / total;
                string bar = new('█', (int)(percent / 2));
                Console.WriteLine($"      {kvp.Key.Replace(".loadbalance.example.com", "")}: {bar} {percent:F1}%");
            }
        }
    }
}