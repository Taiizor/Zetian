using System.Text;

namespace Zetian.WebUI.Examples
{
    /// <summary>
    /// Main program
    /// </summary>
    public class Program
    {
        public static async Task Main()
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("Zetian WebUI Examples");
            Console.WriteLine("====================");
            Console.WriteLine("1. Basic WebUI");
            Console.WriteLine("2. Authenticated WebUI");
            Console.WriteLine("3. Custom Configuration");
            Console.WriteLine("4. Real-Time Monitoring");
            Console.WriteLine("\nSelect an example (1-4):");

            string? choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await BasicWebUIExample.RunAsync();
                        break;
                    case "2":
                        await AuthenticatedWebUIExample.RunAsync();
                        break;
                    case "3":
                        await CustomWebUIExample.RunAsync();
                        break;
                    case "4":
                        await RealTimeMonitoringExample.RunAsync();
                        break;
                    default:
                        Console.WriteLine("Invalid choice!");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
