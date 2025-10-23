using System.Text;

namespace Zetian.Examples
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
            Console.WriteLine("1. Basic SMTP Server");
            Console.WriteLine("2. Authenticated SMTP Server");
            Console.WriteLine("3. Secure SMTP Server with TLS");
            Console.WriteLine("4. Rate Limited SMTP Server");
            Console.WriteLine("5. SMTP Server with Message Storage");
            Console.WriteLine("6. SMTP Server with Custom Processing");
            Console.WriteLine("7. Full Featured SMTP Server");
            Console.WriteLine("8. Protocol-Level vs Event-Based Filtering");
            Console.WriteLine();
            Console.Write("Select an example (1-8): ");

            string? choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await BasicExample.RunAsync();
                        break;
                    case "2":
                        await AuthenticatedExample.RunAsync();
                        break;
                    case "3":
                        await SecureExample.RunAsync();
                        break;
                    case "4":
                        await RateLimitedExample.RunAsync();
                        break;
                    case "5":
                        await MessageStorageExample.RunAsync();
                        break;
                    case "6":
                        await CustomProcessingExample.RunAsync();
                        break;
                    case "7":
                        await FullFeaturedExample.RunAsync();
                        break;
                    case "8":
                        await ProtocolLevelFilteringExample.RunAsync();
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