using System.Text;

namespace Zetian.HealthCheck.Examples
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("Zetian SMTP Server Examples");
            Console.WriteLine("============================");
            Console.WriteLine();
            Console.WriteLine("1. SMTP Server Health Check");
            Console.WriteLine();
            Console.Write("Select an example (1-3): ");

            string? choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await HealthCheckExample.RunAsync();
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