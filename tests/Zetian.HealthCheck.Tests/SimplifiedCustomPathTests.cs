using System.Net;
using Xunit;
using Zetian.Configuration;
using Zetian.HealthCheck.Extensions;

namespace Zetian.HealthCheck.Tests
{
    /// <summary>
    /// Simplified tests to verify custom path behavior
    /// </summary>
    public class SimplifiedCustomPathTests : IDisposable
    {
        private SmtpServer? _smtpServer;
        private HealthCheckService? _healthCheckService;
        private readonly HttpClient _httpClient = new();
        private static int _portCounter = 60000;  // Different range

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

        [Fact]
        public async Task DefaultHealthCheckPath_Works()
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

            // Act & Assert
            await Task.Delay(500);

            // Test standard paths
            HttpResponseMessage response1 = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/health/");
            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

            HttpResponseMessage response2 = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/health");
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

            HttpResponseMessage response3 = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/healthz");
            Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
        }

        [Fact]
        public async Task CustomPrefix_StillUsesStandardPaths()
        {
            // Arrange
            SmtpServerConfiguration config = new()
            {
                Port = GetNextPort()
            };
            _smtpServer = new SmtpServer(config);
            await _smtpServer.StartAsync();

            int healthCheckPort = GetNextPort();

            // Use the overload that takes port and path
            _healthCheckService = _smtpServer.EnableHealthCheck(healthCheckPort, "/api/");
            await _healthCheckService.StartAsync();

            // Act & Assert
            await Task.Delay(500);

            // The health check should be available at /api/health (combining prefix and standard path)
            HttpResponseMessage response1 = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/api/health");
            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

            HttpResponseMessage response2 = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/api/healthz");
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

            HttpResponseMessage response3 = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/api/livez");
            Assert.Equal(HttpStatusCode.OK, response3.StatusCode);

            HttpResponseMessage response4 = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/api/readyz");
            Assert.Equal(HttpStatusCode.OK, response4.StatusCode);
        }

        [Fact]
        public async Task MultipleCustomPrefixes_CanCoexist()
        {
            // Arrange
            SmtpServerConfiguration config = new()
            {
                Port = GetNextPort()
            };
            _smtpServer = new SmtpServer(config);
            await _smtpServer.StartAsync();

            int healthCheckPort = GetNextPort();
            HealthCheckServiceOptions serviceOptions = new()
            {
                Prefixes = new()
                {
                    $"http://localhost:{healthCheckPort}/v1/",
                    $"http://localhost:{healthCheckPort}/api/v1/",
                    $"http://127.0.0.1:{healthCheckPort}/status/"
                }
            };

            _healthCheckService = _smtpServer.EnableHealthCheck(serviceOptions);
            await _healthCheckService.StartAsync();

            // Act & Assert
            await Task.Delay(500);

            // Each prefix should respond to the standard paths
            HttpResponseMessage response1 = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/v1/health");
            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

            HttpResponseMessage response2 = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/api/v1/healthz");
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

            HttpResponseMessage response3 = await _httpClient.GetAsync($"http://127.0.0.1:{healthCheckPort}/status/livez");
            Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
        }
    }
}