using System.Net;
using System.Text.Json;
using Xunit;
using Zetian.Configuration;
using Zetian.HealthCheck.Extensions;

namespace Zetian.HealthCheck.Tests
{
    public class HealthCheckTests : IDisposable
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

        [Fact]
        public async Task HealthCheck_WhenServerIsRunning_ReturnsHealthy()
        {
            // Arrange
            SmtpServerConfiguration config = new()
            {
                Port = new Random().Next(50000, 65536), // Random port
                ServerName = "Test SMTP Server"
            };
            _smtpServer = new SmtpServer(config);
            await _smtpServer.StartAsync();

            _healthCheckService = _smtpServer.EnableHealthCheck(8181);
            await _healthCheckService.StartAsync();

            // Act
            await Task.Delay(500); // Give the service time to start
            HttpResponseMessage response = await _httpClient.GetAsync("http://localhost:8181/health/");
            string content = await response.Content.ReadAsStringAsync();

            // Parse JSON response
            using JsonDocument jsonDoc = JsonDocument.Parse(content);
            JsonElement root = jsonDoc.RootElement;

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", root.GetProperty("status").GetString());
            Assert.True(root.TryGetProperty("checks", out JsonElement checks));
            Assert.True(checks.TryGetProperty("smtp_server", out JsonElement smtpCheck));
            Assert.Equal("Healthy", smtpCheck.GetProperty("status").GetString());
        }

        [Fact]
        public async Task HealthCheck_WhenServerIsStopped_ReturnsUnhealthy()
        {
            // Arrange
            SmtpServerConfiguration config = new()
            {
                Port = new Random().Next(50000, 65536),
                ServerName = "Test SMTP Server"
            };
            _smtpServer = new SmtpServer(config);

            _healthCheckService = _smtpServer.EnableHealthCheck(8182);
            await _healthCheckService.StartAsync();

            // Act
            await Task.Delay(500);
            HttpResponseMessage response = await _httpClient.GetAsync("http://localhost:8182/health/");

            // Assert
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

            string content = await response.Content.ReadAsStringAsync();
            using JsonDocument jsonDoc = JsonDocument.Parse(content);
            JsonElement root = jsonDoc.RootElement;
            Assert.Equal("Unhealthy", root.GetProperty("status").GetString());
        }

        [Fact]
        public async Task LivenessCheck_AlwaysReturnsOK()
        {
            // Arrange
            SmtpServerConfiguration config = new() { Port = new Random().Next(50000, 65536) };
            _smtpServer = new SmtpServer(config);
            _healthCheckService = _smtpServer.EnableHealthCheck(8183);
            await _healthCheckService.StartAsync();

            // Act
            await Task.Delay(500);
            HttpResponseMessage response = await _httpClient.GetAsync("http://localhost:8183/health/livez");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string content = await response.Content.ReadAsStringAsync();
            using JsonDocument jsonDoc = JsonDocument.Parse(content);
            JsonElement root = jsonDoc.RootElement;
            Assert.Equal("Alive", root.GetProperty("status").GetString());
        }

        [Fact]
        public async Task CustomHealthCheck_CanBeAdded()
        {
            // Arrange
            SmtpServerConfiguration config = new() { Port = new Random().Next(50000, 65536) };
            _smtpServer = new SmtpServer(config);
            await _smtpServer.StartAsync();

            _healthCheckService = _smtpServer.EnableHealthCheck(8184);

            // Add custom health check
            _healthCheckService.AddHealthCheck("custom_check", async (ct) =>
            {
                await Task.Delay(10, ct);
                return HealthCheckResult.Healthy("Custom check passed",
                    new Dictionary<string, object>
                    {
                        ["customData"] = "test value"
                    });
            });

            await _healthCheckService.StartAsync();

            // Act
            await Task.Delay(500);
            HttpResponseMessage response = await _httpClient.GetAsync("http://localhost:8184/health/");
            string content = await response.Content.ReadAsStringAsync();

            // Parse JSON response
            using JsonDocument jsonDoc = JsonDocument.Parse(content);
            JsonElement root = jsonDoc.RootElement;

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(root.TryGetProperty("checks", out JsonElement checks));
            Assert.True(checks.TryGetProperty("custom_check", out JsonElement customCheck));
            Assert.Equal("Healthy", customCheck.GetProperty("status").GetString());
            Assert.Equal("Custom check passed", customCheck.GetProperty("description").GetString());
        }

        [Fact]
        public async Task HealthCheck_IncludesServerMetrics()
        {
            // Arrange
            SmtpServerConfiguration config = new()
            {
                Port = new Random().Next(50000, 65536),
                MaxConnections = 100,
                MaxMessageSize = 10485760, // 10MB
                RequireAuthentication = true
            };
            _smtpServer = new SmtpServer(config);
            await _smtpServer.StartAsync();

            _healthCheckService = _smtpServer.EnableHealthCheck(8185);
            await _healthCheckService.StartAsync();

            // Act
            await Task.Delay(500);
            HttpResponseMessage response = await _httpClient.GetAsync("http://localhost:8185/health/");
            string content = await response.Content.ReadAsStringAsync();

            // Parse JSON response
            using JsonDocument jsonDoc = JsonDocument.Parse(content);
            JsonElement root = jsonDoc.RootElement;

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            JsonElement smtpCheck = root.GetProperty("checks").GetProperty("smtp_server");
            JsonElement data = smtpCheck.GetProperty("data");

            Assert.Equal("running", data.GetProperty("status").GetString());
            Assert.True(data.TryGetProperty("configuration", out JsonElement config2));
            Assert.Equal(100, config2.GetProperty("maxConnections").GetInt32());
            Assert.Equal(10485760, config2.GetProperty("maxMessageSize").GetInt64());
            Assert.True(config2.GetProperty("requireAuthentication").GetBoolean());
        }

        [Fact]
        public async Task StartWithHealthCheck_StartsSmtpServerAndHealthCheck()
        {
            // Arrange
            SmtpServerConfiguration config = new() { Port = new Random().Next(50000, 65536) };
            _smtpServer = new SmtpServer(config);

            // Act
            _healthCheckService = await _smtpServer.StartWithHealthCheckAsync(8186);

            // Assert
            Assert.True(_smtpServer.IsRunning);
            Assert.True(_healthCheckService.IsRunning);

            // Verify health endpoint
            await Task.Delay(500);
            HttpResponseMessage response = await _httpClient.GetAsync("http://localhost:8186/health/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task HealthCheck_WithIPBinding_Works()
        {
            // Arrange
            SmtpServerConfiguration config = new() { Port = new Random().Next(50000, 65536) };
            _smtpServer = new SmtpServer(config);
            await _smtpServer.StartAsync();

            // Act - Bind to specific IP
            IPAddress ipAddress = IPAddress.Loopback;
            _healthCheckService = _smtpServer.EnableHealthCheck(ipAddress, 8188);
            await _healthCheckService.StartAsync();

            // Assert
            Assert.True(_healthCheckService.IsRunning);

            await Task.Delay(500);
            HttpResponseMessage response = await _httpClient.GetAsync("http://127.0.0.1:8188/health/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task HealthCheck_WithHostnameBinding_Works()
        {
            // Arrange
            SmtpServerConfiguration config = new() { Port = new Random().Next(50000, 65536) };
            _smtpServer = new SmtpServer(config);
            await _smtpServer.StartAsync();

            // Act - Bind to hostname
            _healthCheckService = _smtpServer.EnableHealthCheck("localhost", 8189);
            await _healthCheckService.StartAsync();

            // Assert
            Assert.True(_healthCheckService.IsRunning);

            await Task.Delay(500);
            HttpResponseMessage response = await _httpClient.GetAsync("http://localhost:8189/health/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task HealthCheckService_CanBeStopped()
        {
            // Arrange
            SmtpServerConfiguration config = new() { Port = new Random().Next(50000, 65536) };
            _smtpServer = new SmtpServer(config);
            await _smtpServer.StartAsync();

            _healthCheckService = _smtpServer.EnableHealthCheck(8187);
            await _healthCheckService.StartAsync();

            await Task.Delay(500);

            // Act
            await _healthCheckService.StopAsync();

            // Assert
            Assert.False(_healthCheckService.IsRunning);

            // Should not be able to connect
            await Assert.ThrowsAsync<HttpRequestException>(async () =>
            {
                await _httpClient.GetAsync("http://localhost:8187/health/");
            });
        }
    }
}