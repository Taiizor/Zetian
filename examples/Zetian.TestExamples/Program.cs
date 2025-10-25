using System.Text;

namespace Zetian.TestExamples
{
    class Program
    {
        static async Task Main()
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("Zetian SMTP Server Test Examples");
            Console.WriteLine("============================");
            Console.WriteLine();
            Console.WriteLine("1. Race Condition Test");
            Console.WriteLine("2. Improved Race Condition Test");
            Console.WriteLine("3. Max Retry Count Test");
            Console.WriteLine();
            Console.Write("Select an example (1-3): ");

            string? choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await RaceConditionTestExample.RunAsync();
                        break;
                    case "2":
                        await ImprovedRaceConditionTestExample.RunAsync();
                        break;
                    case "3":
                        await MaxRetryCountTestExample.RunAsync();
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