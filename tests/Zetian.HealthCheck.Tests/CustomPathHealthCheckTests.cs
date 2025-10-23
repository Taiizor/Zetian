using System.Net;
using System.Text.Json;
using Xunit;
using Zetian.Configuration;
using Zetian.HealthCheck.Extensions;
using Zetian.HealthCheck.Models;
using Zetian.HealthCheck.Options;
using Zetian.HealthCheck.Services;
using Zetian.Server;

namespace Zetian.HealthCheck.Tests
{
    /// <summary>
    /// Tests for health check with custom path configuration
    /// </summary>
    public class CustomPathHealthCheckTests : IDisposable
    {
        private SmtpServer? _smtpServer;
        private HealthCheckService? _healthCheckService;
        private readonly HttpClient _httpClient = new();
        private static int _portCounter = 55000;  // Starting port for custom path tests (unique range)

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
        public async Task HealthCheck_WithCustomPrefix_ShouldWork()
        {
            // Arrange
            SmtpServerConfiguration config = new()
            {
                Port = GetNextPort(),
                ServerName = "Test SMTP Server"
            };
            _smtpServer = new SmtpServer(config);
            await _smtpServer.StartAsync();

            int healthCheckPort = GetNextPort();
            HealthCheckServiceOptions serviceOptions = new()
            {
                Prefixes = new()
                {
                    $"http://localhost:{healthCheckPort}/status/"
                }
            };

            _healthCheckService = _smtpServer.EnableHealthCheck(serviceOptions);
            await _healthCheckService.StartAsync();

            // Act - With new implementation, custom prefix works correctly
            await Task.Delay(500);
            HttpResponseMessage response = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/status/");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string content = await response.Content.ReadAsStringAsync();
            using JsonDocument jsonDoc = JsonDocument.Parse(content);
            JsonElement root = jsonDoc.RootElement;
            Assert.Equal("Healthy", root.GetProperty("status").GetString());
        }

        [Fact]
        public async Task CustomPrefix_LivenessCheck_ShouldWork()
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
                    $"http://localhost:{healthCheckPort}/api/"
                }
            };

            _healthCheckService = _smtpServer.EnableHealthCheck(serviceOptions);
            await _healthCheckService.StartAsync();

            // Act - With new implementation, custom prefix works correctly
            await Task.Delay(500);
            HttpResponseMessage response = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/api/livez");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Alive", content);
        }

        [Fact]
        public async Task CustomPrefix_ReadinessCheck_ShouldWork()
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
                    $"http://localhost:{healthCheckPort}/monitoring/"
                }
            };

            _healthCheckService = _smtpServer.EnableHealthCheck(serviceOptions);
            await _healthCheckService.StartAsync();

            // Act - With new implementation, custom prefix works correctly
            await Task.Delay(500);
            HttpResponseMessage response = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/monitoring/readyz");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task DefaultPath_ShouldNotWork_WhenCustomPathSet()
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
                    $"http://localhost:{healthCheckPort}/custom/"
                }
            };

            _healthCheckService = _smtpServer.EnableHealthCheck(serviceOptions);
            await _healthCheckService.StartAsync();

            // Act
            await Task.Delay(500);

            // Custom path should work
            HttpResponseMessage customResponse = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/custom/");
            Assert.Equal(HttpStatusCode.OK, customResponse.StatusCode);

            // Try to access default path - The behavior depends on HttpListener
            // If the listener is only on /custom/, accessing /health/ might fail or return 404
            try
            {
                HttpResponseMessage healthResponse = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/health/");
                // If we get here, the path returns something (likely 404 Not Found)
                Assert.NotEqual(HttpStatusCode.OK, healthResponse.StatusCode);
            }
            catch (HttpRequestException)
            {
                // This is expected - the listener is not on /health/
                // Test passes
            }
        }

        [Theory]
        [InlineData("/status/")]
        [InlineData("/api/status/")]
        [InlineData("/v1/health/")]
        [InlineData("/healthz/")]
        public async Task DifferentCustomPaths_ShouldWork(string customPath)
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
                    $"http://localhost:{healthCheckPort}{customPath}"
                }
            };

            _healthCheckService = _smtpServer.EnableHealthCheck(serviceOptions);
            await _healthCheckService.StartAsync();

            // Act - With new implementation, custom prefixes work correctly
            await Task.Delay(500);
            HttpResponseMessage response = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}{customPath}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task MultiplePrefixes_ShouldAllWork()
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
                    $"http://localhost:{healthCheckPort}/health/",
                    $"http://localhost:{healthCheckPort}/status/",
                    $"http://127.0.0.1:{healthCheckPort}/api/"
                }
            };

            _healthCheckService = _smtpServer.EnableHealthCheck(serviceOptions);
            await _healthCheckService.StartAsync();

            // Act & Assert
            await Task.Delay(500);

            // Test first prefix
            HttpResponseMessage response1 = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/health/");
            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

            // Test second prefix
            HttpResponseMessage response2 = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/status/");
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

            // Test third prefix
            HttpResponseMessage response3 = await _httpClient.GetAsync($"http://127.0.0.1:{healthCheckPort}/api/");
            Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
        }

        [Fact]
        public async Task CustomPath_WithCustomOptions_ShouldWork()
        {
            // Arrange
            SmtpServerConfiguration config = new()
            {
                Port = GetNextPort(),
                MaxConnections = 50
            };
            _smtpServer = new SmtpServer(config);
            await _smtpServer.StartAsync();

            int healthCheckPort = GetNextPort();
            HealthCheckServiceOptions serviceOptions = new()
            {
                Prefixes = new()
                {
                    $"http://localhost:{healthCheckPort}/metrics/"
                },
                DegradedStatusCode = 218  // Custom degraded status code
            };

            SmtpHealthCheckOptions healthOptions = new()
            {
                DegradedThresholdPercent = 70,
                UnhealthyThresholdPercent = 90
            };

            _healthCheckService = _smtpServer.EnableHealthCheck(serviceOptions, healthOptions);
            await _healthCheckService.StartAsync();

            // Act - With new implementation, custom prefix works
            await Task.Delay(500);
            HttpResponseMessage response = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/metrics/");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string content = await response.Content.ReadAsStringAsync();
            using JsonDocument jsonDoc = JsonDocument.Parse(content);
            JsonElement root = jsonDoc.RootElement;

            // Verify SMTP server health check exists
            Assert.True(root.TryGetProperty("checks", out JsonElement checks));
            Assert.True(checks.TryGetProperty("smtp_server", out _));
        }

        [Fact]
        public async Task CustomPath_WithTrailingSlash_ShouldWork()
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
                    $"http://localhost:{healthCheckPort}/status/"  // With trailing slash
                }
            };

            _healthCheckService = _smtpServer.EnableHealthCheck(serviceOptions);
            await _healthCheckService.StartAsync();

            // Act & Assert
            await Task.Delay(500);

            // Test with trailing slash
            HttpResponseMessage response1 = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/status/");
            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

            // Test without trailing slash  
            HttpResponseMessage response2 = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/status");
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        }

        [Fact]
        public async Task CustomPath_WithCustomHealthChecks_ShouldIncludeThem()
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
                    $"http://localhost:{healthCheckPort}/diagnostics/"
                }
            };

            _healthCheckService = _smtpServer.EnableHealthCheck(serviceOptions);

            // Add custom health check
            _healthCheckService.AddHealthCheck("custom_service", async (ct) =>
            {
                await Task.Delay(10, ct);
                return HealthCheckResult.Healthy("Custom service is healthy",
                    new Dictionary<string, object>
                    {
                        ["version"] = "1.0.0",
                        ["environment"] = "test"
                    });
            });

            await _healthCheckService.StartAsync();

            // Act - With new implementation, custom prefix works
            await Task.Delay(500);
            HttpResponseMessage response = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/diagnostics/");
            string content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using JsonDocument jsonDoc = JsonDocument.Parse(content);
            JsonElement root = jsonDoc.RootElement;

            Assert.True(root.TryGetProperty("checks", out JsonElement checks));
            Assert.True(checks.TryGetProperty("custom_service", out JsonElement customCheck));
            Assert.Equal("Healthy", customCheck.GetProperty("status").GetString());

            JsonElement data = customCheck.GetProperty("data");
            Assert.Equal("1.0.0", data.GetProperty("version").GetString());
            Assert.Equal("test", data.GetProperty("environment").GetString());
        }

        [Fact]
        public async Task StartWithHealthCheck_DefaultAndCustomPath_Comparison()
        {
            // Arrange
            SmtpServerConfiguration config1 = new()
            {
                Port = GetNextPort()
            };
            SmtpServer smtpServer1 = new(config1);

            SmtpServerConfiguration config2 = new()
            {
                Port = GetNextPort()
            };
            _smtpServer = new SmtpServer(config2);

            // Act - Start with default path using StartWithHealthCheckAsync
            int defaultHealthCheckPort = GetNextPort();
            HealthCheckService defaultService = await smtpServer1.StartWithHealthCheckAsync(defaultHealthCheckPort);

            // Start with custom path manually
            int customHealthCheckPort = GetNextPort();
            await _smtpServer.StartAsync();
            HealthCheckServiceOptions serviceOptions = new()
            {
                Prefixes = new()
                {
                    $"http://localhost:{customHealthCheckPort}/status/"
                }
            };
            _healthCheckService = _smtpServer.EnableHealthCheck(serviceOptions);
            await _healthCheckService.StartAsync();

            // Assert
            Assert.True(smtpServer1.IsRunning);
            Assert.True(defaultService.IsRunning);
            Assert.True(_smtpServer.IsRunning);
            Assert.True(_healthCheckService.IsRunning);

            await Task.Delay(500);

            // Test default path
            HttpResponseMessage response1 = await _httpClient.GetAsync($"http://localhost:{defaultHealthCheckPort}/health/");
            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

            // Test custom path
            HttpResponseMessage response2 = await _httpClient.GetAsync($"http://localhost:{customHealthCheckPort}/status/");
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

            // Cleanup
            defaultService?.Dispose();
            smtpServer1?.Dispose();
        }
    }
}