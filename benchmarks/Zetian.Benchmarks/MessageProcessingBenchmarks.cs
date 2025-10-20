using BenchmarkDotNet.Attributes;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace Zetian.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, iterationCount: 5)]
    public class MessageProcessingBenchmarks
    {
        private byte[] _smallMessage = null!;
        private byte[] _mediumMessage = null!;
        private byte[] _largeMessage = null!;

        [Params(1024, 10_240, 102_400)] // 1KB, 10KB, 100KB
        public int MessageSize { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _smallMessage = GenerateEmailMessage(1024);
            _mediumMessage = GenerateEmailMessage(10_240);
            _largeMessage = GenerateEmailMessage(102_400);
        }

        private byte[] GenerateEmailMessage(int size)
        {
            StringBuilder sb = new();
            sb.AppendLine("Subject: Test Message");
            sb.AppendLine("From: sender@example.com");
            sb.AppendLine("To: recipient@example.com");
            sb.AppendLine("Date: " + DateTime.UtcNow.ToString("R"));
            sb.AppendLine("Content-Type: text/plain; charset=utf-8");
            sb.AppendLine();

            // Generate message body
            Random random = new(42);
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 \r\n";

            while (sb.Length < size)
            {
                // Add random lines
                for (int i = 0; i < 10 && sb.Length < size; i++)
                {
                    for (int j = 0; j < 72 && sb.Length < size; j++)
                    {
                        sb.Append(chars[random.Next(chars.Length)]);
                    }
                    sb.AppendLine();
                }

                // Add dot-stuffed line occasionally
                if (random.Next(10) == 0)
                {
                    sb.AppendLine("..This line starts with a dot");
                }
            }

            sb.AppendLine("\r\n.");  // End of message marker

            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        private byte[] GetMessageForCurrentSize()
        {
            return MessageSize switch
            {
                1024 => _smallMessage,
                10_240 => _mediumMessage,
                102_400 => _largeMessage,
                _ => _smallMessage
            };
        }

        [Benchmark(Baseline = true)]
        public async Task<byte[]> ProcessMessage_WithMemoryStream()
        {
            byte[] messageData = GetMessageForCurrentSize();
            using MemoryStream inputStream = new(messageData);
            using MemoryStream outputStream = new();

            byte[] buffer = new byte[4096];
            StringBuilder lineBuilder = new();
            bool previousWasCr = false;

            int bytesRead;
            while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < bytesRead; i++)
                {
                    byte b = buffer[i];

                    if (previousWasCr && b == '\n')
                    {
                        string line = lineBuilder.ToString();
                        lineBuilder.Clear();

                        if (line == ".")
                        {
                            return outputStream.ToArray();
                        }

                        // Remove dot-stuffing
                        if (line.StartsWith(".."))
                        {
                            line = line[1..];
                        }

                        byte[] lineBytes = Encoding.ASCII.GetBytes(line + "\r\n");
                        await outputStream.WriteAsync(lineBytes, 0, lineBytes.Length);
                        previousWasCr = false;
                    }
                    else if (b == '\r')
                    {
                        previousWasCr = true;
                    }
                    else
                    {
                        if (previousWasCr)
                        {
                            lineBuilder.Append('\r');
                            previousWasCr = false;
                        }
                        lineBuilder.Append((char)b);
                    }
                }
            }

            return outputStream.ToArray();
        }

        [Benchmark]
        public async Task<byte[]> ProcessMessage_WithPipelines()
        {
            byte[] messageData = GetMessageForCurrentSize();
            using MemoryStream inputStream = new(messageData);
            PipeReader reader = PipeReader.Create(inputStream);

            ArrayBufferWriter<byte> messageBuffer = new();
            StringBuilder lineBuilder = new();
            bool previousWasCr = false;

            while (true)
            {
                ReadResult result = await reader.ReadAsync();
                ReadOnlySequence<byte> buffer = result.Buffer;

                SequencePosition consumed = buffer.Start;
                SequencePosition examined = buffer.End;

                foreach (ReadOnlyMemory<byte> segment in buffer)
                {
                    ReadOnlySpan<byte> span = segment.Span;

                    for (int i = 0; i < span.Length; i++)
                    {
                        byte b = span[i];

                        if (previousWasCr && b == '\n')
                        {
                            string line = lineBuilder.ToString();
                            lineBuilder.Clear();

                            if (line == ".")
                            {
                                consumed = buffer.GetPosition(i + 1, consumed);
                                reader.AdvanceTo(consumed);
                                return messageBuffer.WrittenMemory.ToArray();
                            }

                            // Remove dot-stuffing
                            if (line.StartsWith(".."))
                            {
                                line = line[1..];
                            }

                            byte[] lineBytes = Encoding.ASCII.GetBytes(line + "\r\n");
                            messageBuffer.Write(lineBytes);
                            previousWasCr = false;
                        }
                        else if (b == '\r')
                        {
                            previousWasCr = true;
                        }
                        else
                        {
                            if (previousWasCr)
                            {
                                lineBuilder.Append('\r');
                                previousWasCr = false;
                            }
                            lineBuilder.Append((char)b);
                        }
                    }

                    consumed = buffer.GetPosition(segment.Length, consumed);
                }

                reader.AdvanceTo(consumed, examined);

                if (result.IsCompleted)
                {
                    return messageBuffer.WrittenMemory.ToArray();
                }
            }
        }

        [Benchmark]
        public async Task<byte[]> ProcessMessage_WithArrayPool()
        {
            byte[] messageData = GetMessageForCurrentSize();
            using MemoryStream inputStream = new(messageData);
            using MemoryStream outputStream = new();

            byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
            try
            {
                StringBuilder lineBuilder = new();
                bool previousWasCr = false;

                int bytesRead;
                while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < bytesRead; i++)
                    {
                        byte b = buffer[i];

                        if (previousWasCr && b == '\n')
                        {
                            string line = lineBuilder.ToString();
                            lineBuilder.Clear();

                            if (line == ".")
                            {
                                return outputStream.ToArray();
                            }

                            // Remove dot-stuffing
                            if (line.StartsWith(".."))
                            {
                                line = line[1..];
                            }

                            byte[] lineBytes = Encoding.ASCII.GetBytes(line + "\r\n");
                            await outputStream.WriteAsync(lineBytes, 0, lineBytes.Length);
                            previousWasCr = false;
                        }
                        else if (b == '\r')
                        {
                            previousWasCr = true;
                        }
                        else
                        {
                            if (previousWasCr)
                            {
                                lineBuilder.Append('\r');
                                previousWasCr = false;
                            }
                            lineBuilder.Append((char)b);
                        }
                    }
                }

                return outputStream.ToArray();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
