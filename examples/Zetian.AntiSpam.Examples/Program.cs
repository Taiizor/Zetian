namespace Zetian.AntiSpam.Examples
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Zetian.AntiSpam Examples");
            Console.WriteLine("========================\n");
            Console.WriteLine("Select an example to run:");
            Console.WriteLine("1. Basic Anti-Spam (default configuration)");
            Console.WriteLine("2. Custom Anti-Spam (advanced configuration)");
            Console.WriteLine("3. Quarantine Mode (quarantine suspicious messages)");
            Console.WriteLine("4. Bayesian Training (train and test Bayesian filter)");
            Console.WriteLine("5. Exit");
            Console.Write("\nEnter your choice (1-5): ");

            string? choice = Console.ReadLine();

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
                        await QuarantineExample.RunAsync();
                        break;
                    case "4":
                        await BayesianTrainingExample.RunAsync();
                        break;
                    case "5":
                        Console.WriteLine("Exiting...");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please run the program again.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }
    }
}