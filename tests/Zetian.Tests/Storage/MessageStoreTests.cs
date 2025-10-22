using System.Net;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;
using Xunit;
using Zetian.Core;
using Zetian.Storage;

namespace Zetian.Tests.Storage
{
    public class MessageStoreTests
    {
        [Fact]
        public async Task FileMessageStore_SavesMessageToDirectory()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), $"zetian_test_{Guid.NewGuid()}");
            FileMessageStore store = new(tempDir, createDirectoryStructure: false);
            MockSession session = new();
            MockMessage message = new();

            try
            {
                // Act
                bool result = await store.SaveAsync(session, message);

                // Assert
                Assert.True(result);
                Assert.True(Directory.Exists(tempDir));

                string[] files = Directory.GetFiles(tempDir, "*.eml");
                Assert.Single(files);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public async Task FileMessageStore_CreatesDateFolders_WhenEnabled()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), $"zetian_test_{Guid.NewGuid()}");
            FileMessageStore store = new(tempDir, createDirectoryStructure: true);
            MockSession session = new();
            MockMessage message = new();

            try
            {
                // Act
                bool result = await store.SaveAsync(session, message);

                // Assert
                Assert.True(result);

                DateTime now = DateTime.Now;
                string expectedPath = Path.Combine(tempDir, now.ToString("yyyy"), now.ToString("MM"), now.ToString("dd"));
                Assert.True(Directory.Exists(expectedPath));

                string[] files = Directory.GetFiles(expectedPath, "*.eml", SearchOption.AllDirectories);
                Assert.Single(files);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public async Task NullMessageStore_AlwaysReturnsTrue()
        {
            // Arrange
            NullMessageStore store = NullMessageStore.Instance;
            MockSession session = new();
            MockMessage message = new();

            // Act
            bool result = await store.SaveAsync(session, message);

            // Assert
            Assert.True(result);
        }

        private class MockSession : ISmtpSession
        {
            public string Id => "test_session";
            public EndPoint RemoteEndPoint => new IPEndPoint(IPAddress.Loopback, 25);
            public EndPoint LocalEndPoint => new IPEndPoint(IPAddress.Any, 25);
            public bool IsSecure => false;
            public bool IsAuthenticated => false;
            public string? AuthenticatedIdentity => null;
            public string? ClientDomain => "test.local";
            public DateTime StartTime => DateTime.UtcNow;
            public IDictionary<string, object> Properties => new Dictionary<string, object>();
            public X509Certificate2? ClientCertificate => null;
            public int MessageCount => 0;
            public bool PipeliningEnabled { get; set; }
            public bool EightBitMimeEnabled { get; set; }
            public bool BinaryMimeEnabled { get; set; }
            public long MaxMessageSize { get; set; } = 10485760;
        }

        private class MockMessage : ISmtpMessage
        {
            public string Id => "test_message_123";
            public string SessionId => "test_session";
            public MailAddress? From => new("sender@test.com");
            public IReadOnlyList<MailAddress> Recipients => new[]
            {
                new MailAddress("recipient@test.com")
            };
            public long Size => 1024;
            public string? Subject => "Test Subject";
            public string? TextBody => "Test body";
            public string? HtmlBody => "<p>Test body</p>";
            public bool HasAttachments => false;
            public int AttachmentCount => 0;
            public DateTime? Date => DateTime.UtcNow;
            public MailPriority Priority => MailPriority.Normal;
            public IDictionary<string, string> Headers => new Dictionary<string, string>
            {
                ["Subject"] = "Test Subject"
            };

            public byte[] GetRawData()
            {
                return System.Text.Encoding.UTF8.GetBytes("Test message data");
            }

            public Task<byte[]> GetRawDataAsync()
            {
                return Task.FromResult(GetRawData());
            }

            public Stream GetRawDataStream()
            {
                return new MemoryStream(GetRawData());
            }

            public string GetHeader(string name)
            {
                return name == "Subject" ? "Test Subject" : string.Empty;
            }

            public IEnumerable<string> GetHeaders(string name)
            {
                return new[] { GetHeader(name) };
            }

            public void SaveToFile(string path)
            {
                File.WriteAllBytes(path, GetRawData());
            }

            public Task SaveToFileAsync(string path)
            {
                File.WriteAllBytes(path, GetRawData());
                return Task.CompletedTask;
            }

            public void SaveToStream(Stream stream)
            {
                byte[] data = GetRawData();
                stream.Write(data, 0, data.Length);
            }

            public Task SaveToStreamAsync(Stream stream)
            {
                byte[] data = GetRawData();
                return stream.WriteAsync(data, 0, data.Length);
            }
        }
    }
}