using System.Text;

namespace Zetian.Monitoring.Examples
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("Zetian SMTP Server - Monitoring Examples");
            Console.WriteLine("=========================================\n");

            Console.WriteLine("Choose an example to run:");
            Console.WriteLine("1. Basic Monitoring with Statistics");
            Console.WriteLine("2. Prometheus Metrics Export");
            Console.WriteLine("3. Real-time Performance Monitoring");
            Console.WriteLine("4. Custom Metrics Collection");
            Console.WriteLine("Q. Quit\n");

            while (true)
            {
                Console.Write("Select option: ");
                ConsoleKeyInfo key = Console.ReadKey();
                Console.WriteLine();

                try
                {
                    switch (key.KeyChar)
                    {
                        case '1':
                            await BasicMonitoringExample.RunAsync();
                            break;

                        case '2':
                            await PrometheusExample.RunAsync();
                            break;

                        case '3':
                            await PerformanceMonitoringExample.RunAsync();
                            break;

                        case '4':
                            await CustomMetricsExample.RunAsync();
                            break;

                        case 'q':
                        case 'Q':
                            Console.WriteLine("\nExiting...");
                            return;

                        default:
                            Console.WriteLine("Invalid option. Please try again.\n");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nError: {ex.Message}\n");
                }

                Console.WriteLine("\nPress any key to return to menu...");
                Console.ReadKey();
                Console.Clear();

                Console.WriteLine("Choose an example to run:");
                Console.WriteLine("1. Basic Monitoring with Statistics");
                Console.WriteLine("2. Prometheus Metrics Export");
                Console.WriteLine("3. Real-time Performance Monitoring");
                Console.WriteLine("4. Custom Metrics Collection");
                Console.WriteLine("Q. Quit\n");
            }
        }
    }
}