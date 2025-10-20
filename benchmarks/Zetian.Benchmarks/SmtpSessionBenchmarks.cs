using BenchmarkDotNet.Attributes;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace Zetian.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, iterationCount: 10)]
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
        public async Task<List<string>> ReadWithPipeReader()
        {
            _memoryStream.Position = 0;
            PipeReader reader = PipeReader.Create(_memoryStream);

            List<string> lines = new();
            StringBuilder lineBuilder = new();

            while (true)
            {
                ReadResult result = await reader.ReadAsync();
                ReadOnlySequence<byte> buffer = result.Buffer;

                SequencePosition? position = null;

                do
                {
                    position = buffer.PositionOf((byte)'\n');

                    if (position != null)
                    {
                        ReadOnlySequence<byte> line = buffer.Slice(0, position.Value);

                        if (!line.IsEmpty)
                        {
                            byte[] lineBytes = ArrayPool<byte>.Shared.Rent((int)line.Length);
                            try
                            {
                                line.CopyTo(lineBytes);
                                int length = (int)line.Length;
                                if (length > 0 && lineBytes[length - 1] == '\r')
                                {
                                    length--;
                                }
                                lines.Add(Encoding.ASCII.GetString(lineBytes, 0, length));
                            }
                            finally
                            {
                                ArrayPool<byte>.Shared.Return(lineBytes);
                            }
                        }

                        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                    }
                }
                while (position != null);

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
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
        public async Task WriteWithPipeWriter()
        {
            using MemoryStream stream = new();
            PipeWriter writer = PipeWriter.Create(stream);

            for (int i = 0; i < 100; i++)
            {
                string message = $"250 OK - Line {i}\r\n";
                byte[] bytes = Encoding.ASCII.GetBytes(message);
                await writer.WriteAsync(bytes.AsMemory());
                await writer.FlushAsync();
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _memoryStream?.Dispose();
        }
    }
}