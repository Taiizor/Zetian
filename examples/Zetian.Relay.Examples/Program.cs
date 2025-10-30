using Microsoft.Extensions.Logging;
using System.Text;

namespace Zetian.Relay.Examples
{
    public class Program
    {
        public static async Task Main()
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("Zetian SMTP Server - Monitoring Examples");
            Console.WriteLine("=========================================\n");

            Console.WriteLine("Select an example to run:");
            Console.WriteLine("1. Basic Relay Example");
            Console.WriteLine("2. Smart Host Example");
            Console.WriteLine("3. Multiple Smart Hosts with Failover");
            Console.WriteLine("4. Domain Routing Example");
            Console.WriteLine("5. Queue Management Example");
            Console.WriteLine("6. Priority Queue Example");
            Console.WriteLine("7. MX Routing Example");
            Console.WriteLine("8. Load Balancing Example");
            Console.WriteLine("9. Authentication Example");
            Console.WriteLine("10. Custom Relay Configuration");
            Console.WriteLine("0. Exit");

            Console.WriteLine();

            Console.Write("Enter your choice: ");

            string? choice = Console.ReadLine();
            Console.Clear();

            // Create logger
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Information)
                    .AddConsole();
            });

            try
            {
                switch (choice)
                {
                    case "1":
                        await BasicRelayExample.RunAsync(loggerFactory);
                        break;
                    case "2":
                        await SmartHostExample.RunAsync(loggerFactory);
                        break;
                    case "3":
                        await FailoverExample.RunAsync(loggerFactory);
                        break;
                    case "4":
                        await DomainRoutingExample.RunAsync(loggerFactory);
                        break;
                    case "5":
                        await QueueManagementExample.RunAsync(loggerFactory);
                        break;
                    case "6":
                        await PriorityQueueExample.RunAsync(loggerFactory);
                        break;
                    case "7":
                        await MxRoutingExample.RunAsync(loggerFactory);
                        break;
                    case "8":
                        await LoadBalancingExample.RunAsync(loggerFactory);
                        break;
                    case "9":
                        await AuthenticationExample.RunAsync(loggerFactory);
                        break;
                    case "10":
                        await CustomConfigurationExample.RunAsync(loggerFactory);
                        break;
                    case "0":
                        Console.WriteLine("Exiting...");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}