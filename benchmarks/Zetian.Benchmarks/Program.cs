using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Zetian.Benchmarks
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Run benchmarks
            ManualConfig config = DefaultConfig.Instance
                .WithOptions(ConfigOptions.DisableOptimizationsValidator);

            Console.WriteLine("Starting Zetian SMTP Server Benchmarks...");
            Console.WriteLine("========================================");

            BenchmarkRunner.Run<StorageBenchmarks>(config);
            BenchmarkRunner.Run<FilteringBenchmarks>(config);
            BenchmarkRunner.Run<SmtpSessionBenchmarks>(config);
            BenchmarkRunner.Run<AuthenticationBenchmarks>(config);
            BenchmarkRunner.Run<MessageProcessingBenchmarks>(config);

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}