using System.Text;

namespace Zetian.AntiSpam.Examples
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            while (true)
            {
                Console.WriteLine("Zetian SMTP Server - Anti Spam Examples");
                Console.WriteLine("=========================================\n");

                Console.WriteLine("Select an example to run:");
                Console.WriteLine("1. Basic Anti-Spam Setup");
                Console.WriteLine("2. Custom Anti-Spam Configuration");
                Console.WriteLine("3. Bayesian Filter Training");
                Console.WriteLine("4. Email Authentication (SPF/DKIM/DMARC)");
                Console.WriteLine("5. Individual Feature Examples");
                Console.WriteLine("6. Exit");
                Console.Write("\nChoice: ");

                string? choice = Console.ReadLine();
                Console.Clear();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            await BasicAntiSpamExample.RunAsync();
                            break;
                        case "2":
                            await CustomAntiSpamExample.RunAsync();
                            break;
                        case "3":
                            await BayesianTrainingExample.RunAsync();
                            break;
                        case "4":
                            await EmailAuthenticationExample.RunAsync();
                            break;
                        case "5":
                            await IndividualFeaturesExample.RunAsync();
                            break;
                        case "6":
                            Console.WriteLine("Goodbye!");
                            return;
                        default:
                            Console.WriteLine("Invalid choice. Please try again.\n");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\nError: {ex.Message}");
                    Console.ResetColor();
                }

                if (choice != "6")
                {
                    Console.WriteLine("\nPress any key to return to menu...");
                    Console.ReadKey();
                    Console.Clear();
                }
            }
        }
    }
}