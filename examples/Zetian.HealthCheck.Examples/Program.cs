using System.Security.Principal;
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

            // Check if running as admin
            bool isAdmin = false;
            if (OperatingSystem.IsWindows())
            {
                using WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new(identity);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }

            if (!isAdmin && OperatingSystem.IsWindows())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠  Note: Some examples may require administrator privileges on Windows.");
                Console.WriteLine("   Run this application as Administrator for full functionality.\n");
                Console.ResetColor();
            }

            Console.WriteLine("1. Basic Health Check (localhost - usually works without admin)");
            Console.WriteLine("2. Health Check with Custom Options (localhost - usually works without admin)");
            Console.WriteLine("3. Health Check with IP/Hostname Binding (may require admin)");
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