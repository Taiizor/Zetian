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
                Port = new Random().Next(50000, 65536)
            };
            _smtpServer = new SmtpServer(config);
            await _smtpServer.StartAsync();

            _healthCheckService = _smtpServer.EnableHealthCheck(8190);
            await _healthCheckService.StartAsync();

            // Act
            await Task.Delay(500);
            HttpResponseMessage response = await _httpClient.GetAsync($"http://localhost:8190{path}");

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
                Port = new Random().Next(50000, 65536)
            };
            _smtpServer = new SmtpServer(config);
            await _smtpServer.StartAsync();

            _healthCheckService = _smtpServer.EnableHealthCheck(8191);
            await _healthCheckService.StartAsync();

            // Act
            await Task.Delay(500);
            HttpResponseMessage response = await _httpClient.GetAsync($"http://localhost:8191{path}");

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
                Port = new Random().Next(50000, 65536)
            };
            _smtpServer = new SmtpServer(config);
            await _smtpServer.StartAsync();

            _healthCheckService = _smtpServer.EnableHealthCheck(8192);
            await _healthCheckService.StartAsync();

            // Act
            await Task.Delay(500);
            HttpResponseMessage response = await _httpClient.GetAsync($"http://localhost:8192{path}");

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
                Port = new Random().Next(50000, 65536)
            };
            _smtpServer = new SmtpServer(config);
            await _smtpServer.StartAsync();

            _healthCheckService = _smtpServer.EnableHealthCheck(8193);
            await _healthCheckService.StartAsync();

            // Act
            await Task.Delay(500);
            HttpResponseMessage response = await _httpClient.GetAsync("http://localhost:8193/health/invalid");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
