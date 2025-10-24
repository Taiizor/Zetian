using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Zetian.Compare.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Run benchmarks
            ManualConfig config = DefaultConfig.Instance
                .WithOptions(ConfigOptions.DisableOptimizationsValidator);

            BenchmarkRunner.Run<SimpleBenchmark>(config);

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}