using System.Net;
using Xunit;
using Zetian.Configuration;
using Zetian.HealthCheck.Extensions;

namespace Zetian.HealthCheck.Tests
{
    /// <summary>
    /// Tests to verify all health check paths work correctly
    /// </summary>
    public class HealthCheckPathTests : IDisposable
    {
        private SmtpServer? _smtpServer;
        private HealthCheckService? _healthCheckService;
        private readonly HttpClient _httpClient = new();
        private static int _portCounter = 45000;  // Starting port for path tests (different range)

        private static int GetNextPort()
        {
            return Interlocked.Increment(ref _portCounter);
        }

        public void Dispose()
        {
            _healthCheckService?.Dispose();
            _smtpServer?.Dispose();
            _httpClient?.Dispose();
        }

        [Theory]
        [InlineData("/health/")]
        [InlineData("/health")]
        public async Task HealthCheckPath_ReturnsHealthStatus(string path)
        {
            // Arrange
            SmtpServerConfiguration config = new()
            {
                Port = GetNextPort()
            };
            _smtpServer = new SmtpServer(config);
            await _smtpServer.StartAsync();

            int healthCheckPort = GetNextPort();
            _healthCheckService = _smtpServer.EnableHealthCheck(healthCheckPort);
            await _healthCheckService.StartAsync();

            // Act
            await Task.Delay(500);
            HttpResponseMessage response = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}{path}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string content = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"status\"", content);
            Assert.Contains("\"checks\"", content);
        }

        [Theory]
        [InlineData("/health/livez")]
        [InlineData("/health/live")]
        public async Task LivenessCheckPaths_ReturnAlive(string path)
        {
            // Arrange
            SmtpServerConfiguration config = new()
            {
                Port = GetNextPort()
            };
            _smtpServer = new SmtpServer(config);
            await _smtpServer.StartAsync();

            int healthCheckPort = GetNextPort();
            _healthCheckService = _smtpServer.EnableHealthCheck(healthCheckPort);
            await _healthCheckService.StartAsync();

            // Act
            await Task.Delay(500);
            HttpResponseMessage response = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}{path}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string content = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"status\"", content);
            Assert.Contains("Alive", content);
        }

        [Theory]
        [InlineData("/health/readyz")]
        [InlineData("/health/ready")]
        public async Task ReadinessCheckPaths_ReturnReady(string path)
        {
            // Arrange
            SmtpServerConfiguration config = new()
            {
                Port = GetNextPort()
            };
            _smtpServer = new SmtpServer(config);
            await _smtpServer.StartAsync();

            int healthCheckPort = GetNextPort();
            _healthCheckService = _smtpServer.EnableHealthCheck(healthCheckPort);
            await _healthCheckService.StartAsync();

            // Act
            await Task.Delay(500);
            HttpResponseMessage response = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}{path}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string content = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"status\"", content);
        }

        [Fact]
        public async Task InvalidPath_Returns404()
        {
            // Arrange
            SmtpServerConfiguration config = new()
            {
                Port = GetNextPort()
            };
            _smtpServer = new SmtpServer(config);
            await _smtpServer.StartAsync();

            int healthCheckPort = GetNextPort();
            _healthCheckService = _smtpServer.EnableHealthCheck(healthCheckPort);
            await _healthCheckService.StartAsync();

            // Act
            await Task.Delay(500);
            HttpResponseMessage response = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/health/invalidpath");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}