using BenchmarkDotNet.Attributes;
using Moq;
using System.Text;
using Zetian.Abstractions;
using Zetian.Authentication;
using Zetian.Delegates;
using Zetian.Models;

namespace Zetian.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, iterationCount: 10)]
    public class AuthenticationBenchmarks
    {
        private PlainAuthenticator _plainAuthenticator = null!;
        private LoginAuthenticator _loginAuthenticator = null!;
        private ISmtpSession _mockSession = null!;
        private string _plainCredentials = null!;
        private byte[] _loginUsernameData = null!;
        private byte[] _loginPasswordData = null!;

        [GlobalSetup]
        public void Setup()
        {
            // Setup mock authentication handler
            Mock<AuthenticationHandler> handler = new();
            handler.Setup(h => h(It.IsAny<string>(), It.IsAny<string>()))
                   .ReturnsAsync(AuthenticationResult.Succeed("testuser"));

            _plainAuthenticator = new PlainAuthenticator(handler.Object);
            _loginAuthenticator = new LoginAuthenticator(handler.Object);
            _mockSession = Mock.Of<ISmtpSession>();

            // Prepare test data
            _plainCredentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes("\0testuser\0password"));

            string username = Convert.ToBase64String(Encoding.ASCII.GetBytes("testuser"));
            string password = Convert.ToBase64String(Encoding.ASCII.GetBytes("password"));
            _loginUsernameData = Encoding.ASCII.GetBytes($"{username}\r\n");
            _loginPasswordData = Encoding.ASCII.GetBytes($"{password}\r\n");
        }

        [Benchmark(Baseline = true)]
        public async Task<AuthenticationResult> PlainAuth_Direct()
        {
            // Direct authentication without I/O simulation
            byte[] bytes = Convert.FromBase64String(_plainCredentials);
            string decoded = Encoding.ASCII.GetString(bytes);
            string[] parts = decoded.Split('\0');

            string username = parts.Length == 2 ? parts[0] : parts[1];
            string password = parts.Length == 2 ? parts[1] : parts[2];

            // Simulate authentication
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                return AuthenticationResult.Succeed(username);
            }

            return AuthenticationResult.Fail("Invalid credentials");
        }

        [Benchmark]
        public async Task<AuthenticationResult> PlainAuth_WithStreamReaderWriter()
        {
            using MemoryStream inputStream = new();
            using MemoryStream outputStream = new();
            using StreamReader reader = new(inputStream, Encoding.ASCII);
            using StreamWriter writer = new(outputStream, Encoding.ASCII);

            AuthenticationResult result = await _plainAuthenticator.AuthenticateAsync(
                _mockSession, _plainCredentials, reader, writer, CancellationToken.None);

            return result;
        }

        [Benchmark]
        public async Task<AuthenticationResult> LoginAuth_Direct()
        {
            // Direct authentication simulation
            string usernameBase64 = Encoding.ASCII.GetString(_loginUsernameData).TrimEnd('\r', '\n');
            string passwordBase64 = Encoding.ASCII.GetString(_loginPasswordData).TrimEnd('\r', '\n');

            string username = Encoding.ASCII.GetString(Convert.FromBase64String(usernameBase64));
            string password = Encoding.ASCII.GetString(Convert.FromBase64String(passwordBase64));

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                return AuthenticationResult.Succeed(username);
            }

            return AuthenticationResult.Fail("Invalid credentials");
        }

        [Benchmark]
        public async Task<AuthenticationResult> LoginAuth_WithStreamReaderWriter()
        {
            using MemoryStream inputStream = new();
            await inputStream.WriteAsync(_loginUsernameData);
            await inputStream.WriteAsync(_loginPasswordData);
            inputStream.Position = 0;

            using MemoryStream outputStream = new();
            using StreamReader reader = new(inputStream, Encoding.ASCII);
            using StreamWriter writer = new(outputStream, Encoding.ASCII);

            AuthenticationResult result = await _loginAuthenticator.AuthenticateAsync(
                _mockSession, null, reader, writer, CancellationToken.None);

            return result;
        }
    }
}