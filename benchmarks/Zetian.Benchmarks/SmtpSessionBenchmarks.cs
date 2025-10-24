using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using System.Buffers;
using System.Text;

namespace Zetian.Benchmarks
{
    [RankColumn]
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [SimpleJob(launchCount: 1, warmupCount: 2, iterationCount: 3)]
    public class SmtpSessionBenchmarks
    {
        private byte[] _testData = null!;
        private MemoryStream _memoryStream = null!;
        private const int MessageSize = 10_000; // 10 KB message

        [GlobalSetup]
        public void Setup()
        {
            // Create test SMTP message data
            StringBuilder sb = new();
            sb.AppendLine("HELO test.example.com");
            sb.AppendLine("MAIL FROM:<sender@example.com>");
            sb.AppendLine("RCPT TO:<recipient@example.com>");
            sb.AppendLine("DATA");

            // Add message content
            for (int i = 0; i < 100; i++)
            {
                sb.AppendLine($"This is line {i} of the test message. Lorem ipsum dolor sit amet, consectetur adipiscing elit.");
            }
            sb.AppendLine(".");
            sb.AppendLine("QUIT");

            _testData = Encoding.ASCII.GetBytes(sb.ToString());
            _memoryStream = new MemoryStream(_testData);
        }

        [Benchmark(Baseline = true)]
        public async Task<List<string>> ReadWithStreamReader()
        {
            _memoryStream.Position = 0;
            using StreamReader reader = new(_memoryStream, Encoding.ASCII, leaveOpen: true);

            List<string> lines = new();
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                lines.Add(line);
            }

            return lines;
        }

        [Benchmark]
        public async Task<List<string>> ReadWithBufferedStream()
        {
            _memoryStream.Position = 0;
            using BufferedStream bufferedStream = new(_memoryStream, 4096);
            using StreamReader reader = new(bufferedStream, Encoding.ASCII, leaveOpen: true);

            List<string> lines = new();
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                lines.Add(line);
            }

            return lines;
        }

        [Benchmark]
        public async Task<List<string>> ReadWithArrayPool()
        {
            _memoryStream.Position = 0;
            List<string> lines = new();
            byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
            try
            {
                StringBuilder lineBuilder = new();
                int bytesRead;
                while ((bytesRead = await _memoryStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < bytesRead; i++)
                    {
                        byte b = buffer[i];
                        if (b == '\n')
                        {
                            if (lineBuilder.Length > 0 && lineBuilder[^1] == '\r')
                            {
                                lineBuilder.Length--; // Remove \r
                            }
                            lines.Add(lineBuilder.ToString());
                            lineBuilder.Clear();
                        }
                        else
                        {
                            lineBuilder.Append((char)b);
                        }
                    }
                }

                if (lineBuilder.Length > 0)
                {
                    lines.Add(lineBuilder.ToString());
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            return lines;
        }

        [Benchmark]
        public async Task WriteWithStreamWriter()
        {
            using MemoryStream stream = new();
            using StreamWriter writer = new(stream, Encoding.ASCII);

            for (int i = 0; i < 100; i++)
            {
                await writer.WriteLineAsync($"250 OK - Line {i}");
                await writer.FlushAsync();
            }
        }

        [Benchmark]
        public async Task WriteWithBufferedStream()
        {
            using MemoryStream stream = new();
            using BufferedStream bufferedStream = new(stream, 4096);
            using StreamWriter writer = new(bufferedStream, Encoding.ASCII);

            for (int i = 0; i < 100; i++)
            {
                await writer.WriteLineAsync($"250 OK - Line {i}");
                await writer.FlushAsync();
            }
        }

        [Benchmark]
        public async Task WriteWithArrayPool()
        {
            using MemoryStream stream = new();
            byte[] buffer = ArrayPool<byte>.Shared.Rent(256);
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    string message = $"250 OK - Line {i}\r\n";
                    byte[] bytes = Encoding.ASCII.GetBytes(message);
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _memoryStream?.Dispose();
        }
    }
}