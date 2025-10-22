using System.Text;

namespace Zetian.HealthCheck.Examples
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("Zetian Health Check Examples");
            Console.WriteLine("=============================");
            Console.WriteLine();
            Console.WriteLine("1. Basic Health Check");
            Console.WriteLine("2. Health Check with Custom Options");
            Console.WriteLine("3. Health Check with IP/Hostname Binding");
            Console.WriteLine();
            Console.Write("Select an example (1-3): ");

            string? choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await BasicHealthCheckExample.RunAsync();
                        break;
                    case "2":
                        await CustomHealthCheckExample.RunAsync();
                        break;
                    case "3":
                        await HealthCheckWithBindingExample.RunAsync();
                        break;
                    default:
                        Console.WriteLine("Invalid choice");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}