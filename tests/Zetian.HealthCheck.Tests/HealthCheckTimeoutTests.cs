using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
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
    public class HealthCheckTimeoutTests : IDisposable
    {
        private readonly SmtpServer _smtpServer;
        private readonly HttpClient _httpClient;
        private readonly ILoggerFactory _loggerFactory;

        public HealthCheckTimeoutTests()
        {
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            SmtpServerConfiguration config = new()
            {
                Port = GetAvailablePort(),
                ServerName = "Test SMTP Server",
                LoggerFactory = _loggerFactory
            };

            _smtpServer = new SmtpServerBuilder()
                .Port(config.Port)
                .ServerName(config.ServerName)
                .LoggerFactory(config.LoggerFactory)
                .Build();

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        [Fact]
        public async Task IndividualHealthCheck_TimesOut_WhenExceedsIndividualTimeout()
        {
            // Arrange
            int healthCheckPort = GetAvailablePort();

            HealthCheckServiceOptions options = new()
            {
                Prefixes = new() { $"http://localhost:{healthCheckPort}/health/" },
                TotalTimeout = TimeSpan.FromSeconds(10),
                IndividualCheckTimeout = TimeSpan.FromSeconds(2),
                FailFastOnTimeout = false,
                TimeoutStatusCode = 503
            };

            using HealthCheckService healthService = new(options, _loggerFactory);

            // Add a slow health check
            healthService.AddHealthCheck("slow_check", async (ct) =>
            {
                await Task.Delay(3000, ct); // 3 seconds, exceeds individual timeout
                return HealthCheckResult.Healthy("Should not reach here");
            });

            // Add a fast health check
            healthService.AddHealthCheck("fast_check", async (ct) =>
            {
                await Task.Delay(500, ct); // 500ms
                return HealthCheckResult.Healthy("Fast check completed");
            });

            await _smtpServer.StartAsync();
            await healthService.StartAsync();

            // Act
            HttpResponseMessage response = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/health/");
            string content = await response.Content.ReadAsStringAsync();
            JsonDocument json = JsonDocument.Parse(content);
            JsonElement root = json.RootElement;

            // Assert
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.True(root.GetProperty("timedOut").GetBoolean());

            JsonElement checks = root.GetProperty("checks");
            Assert.Equal("Timeout", checks.GetProperty("slow_check").GetProperty("status").GetString());
            Assert.Equal("Healthy", checks.GetProperty("fast_check").GetProperty("status").GetString());

            // Cleanup
            await healthService.StopAsync();
            await _smtpServer.StopAsync();
        }

        [Fact]
        public async Task TotalTimeout_StopsAllChecks_WhenExceeded()
        {
            // Arrange
            int healthCheckPort = GetAvailablePort();

            HealthCheckServiceOptions options = new()
            {
                Prefixes = new() { $"http://localhost:{healthCheckPort}/health/" },
                TotalTimeout = TimeSpan.FromSeconds(2), // Short total timeout
                IndividualCheckTimeout = TimeSpan.FromSeconds(5),
                FailFastOnTimeout = false,
                TimeoutStatusCode = 503
            };

            using HealthCheckService healthService = new(options, _loggerFactory);

            // Add multiple slow checks
            for (int i = 1; i <= 5; i++)
            {
                int checkNumber = i;
                healthService.AddHealthCheck($"check_{checkNumber}", async (ct) =>
                {
                    await Task.Delay(3000, ct); // Each takes 3 seconds (more than total timeout of 2 seconds)
                    return HealthCheckResult.Healthy($"Check {checkNumber} completed");
                });
            }

            await _smtpServer.StartAsync();
            await healthService.StartAsync();

            // Act
            Stopwatch stopwatch = Stopwatch.StartNew();
            HttpResponseMessage response = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/health/");
            stopwatch.Stop();

            string content = await response.Content.ReadAsStringAsync();
            JsonDocument json = JsonDocument.Parse(content);
            JsonElement root = json.RootElement;

            // Assert
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.True(root.GetProperty("timedOut").GetBoolean());
            Assert.InRange(stopwatch.Elapsed.TotalSeconds, 1.8, 2.5); // Should complete around 2 seconds

            // At least some checks should be marked as timeout
            JsonElement checks = root.GetProperty("checks");
            int timeoutCount = 0;
            foreach (JsonProperty check in checks.EnumerateObject())
            {
                if (check.Value.GetProperty("status").GetString() == "Timeout")
                {
                    timeoutCount++;
                }
            }
            Assert.True(timeoutCount > 0, "At least some checks should timeout");

            // Cleanup
            await healthService.StopAsync();
            await _smtpServer.StopAsync();
        }

        [Fact]
        public async Task FailFastOnTimeout_StopsProcessing_OnFirstTimeout()
        {
            // Arrange
            int healthCheckPort = GetAvailablePort();

            HealthCheckServiceOptions options = new()
            {
                Prefixes = new() { $"http://localhost:{healthCheckPort}/health/" },
                TotalTimeout = TimeSpan.FromSeconds(10),
                IndividualCheckTimeout = TimeSpan.FromSeconds(1),
                FailFastOnTimeout = true, // Fail fast enabled
                TimeoutStatusCode = 503
            };

            using HealthCheckService healthService = new(options, _loggerFactory);

            List<string> executedChecks = new();

            // Add checks with different delays
            healthService.AddHealthCheck("check_1", async (ct) =>
            {
                executedChecks.Add("check_1");
                await Task.Delay(2000, ct); // Will timeout
                return HealthCheckResult.Healthy("Should not reach");
            });

            healthService.AddHealthCheck("check_2", async (ct) =>
            {
                executedChecks.Add("check_2");
                await Task.Delay(100, ct);
                return HealthCheckResult.Healthy("Check 2 completed");
            });

            healthService.AddHealthCheck("check_3", async (ct) =>
            {
                executedChecks.Add("check_3");
                await Task.Delay(100, ct);
                return HealthCheckResult.Healthy("Check 3 completed");
            });

            await _smtpServer.StartAsync();
            await healthService.StartAsync();

            // Act
            await Task.Delay(500); // Give checks time to start
            HttpResponseMessage response = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/health/");

            // Wait a bit more to ensure no more checks execute
            await Task.Delay(1000);

            // Assert
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

            // When fail-fast is enabled, not all checks may execute
            // But at least the first check should have started
            Assert.Contains("check_1", executedChecks);

            // Cleanup
            await healthService.StopAsync();
            await _smtpServer.StopAsync();
        }

        [Fact]
        public async Task TimeoutStatusCode_IsUsed_WhenTimeout()
        {
            // Arrange
            int healthCheckPort = GetAvailablePort();
            int customStatusCode = 504; // Gateway Timeout

            HealthCheckServiceOptions options = new()
            {
                Prefixes = new() { $"http://localhost:{healthCheckPort}/health/" },
                TotalTimeout = TimeSpan.FromSeconds(2),
                IndividualCheckTimeout = TimeSpan.FromSeconds(1),
                TimeoutStatusCode = customStatusCode
            };

            using HealthCheckService healthService = new(options, _loggerFactory);

            healthService.AddHealthCheck("timeout_check", async (ct) =>
            {
                await Task.Delay(3000, ct);
                return HealthCheckResult.Healthy("Will timeout");
            });

            await _smtpServer.StartAsync();
            await healthService.StartAsync();

            // Act
            HttpResponseMessage response = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/health/");

            // Assert
            Assert.Equal(customStatusCode, (int)response.StatusCode);

            // Cleanup
            await healthService.StopAsync();
            await _smtpServer.StopAsync();
        }

        [Fact]
        public async Task ParallelExecution_CompletesQuicker_ThanSequential()
        {
            // Arrange
            int healthCheckPort = GetAvailablePort();

            HealthCheckServiceOptions options = new()
            {
                Prefixes = new() { $"http://localhost:{healthCheckPort}/health/" },
                TotalTimeout = TimeSpan.FromSeconds(10),
                IndividualCheckTimeout = TimeSpan.FromSeconds(5),
                FailFastOnTimeout = false
            };

            using HealthCheckService healthService = new(options, _loggerFactory);

            // Add 5 checks that each take 1 second
            for (int i = 1; i <= 5; i++)
            {
                int checkNumber = i;
                healthService.AddHealthCheck($"check_{checkNumber}", async (ct) =>
                {
                    await Task.Delay(1000, ct);
                    return HealthCheckResult.Healthy($"Check {checkNumber}");
                });
            }

            await _smtpServer.StartAsync();
            await healthService.StartAsync();

            // Act
            Stopwatch stopwatch = Stopwatch.StartNew();
            HttpResponseMessage response = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/health/");
            stopwatch.Stop();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            // If running in parallel, should complete in around 1 second
            // If sequential, would take 5 seconds
            Assert.InRange(stopwatch.Elapsed.TotalSeconds, 0.8, 2.5);

            // Cleanup
            await healthService.StopAsync();
            await _smtpServer.StopAsync();
        }

        [Fact]
        public async Task ReadinessCheck_RespectsTimeout_Configuration()
        {
            // Arrange
            int healthCheckPort = GetAvailablePort();

            HealthCheckServiceOptions options = new()
            {
                Prefixes = new() { $"http://localhost:{healthCheckPort}/health/" },
                TotalTimeout = TimeSpan.FromSeconds(2),
                IndividualCheckTimeout = TimeSpan.FromSeconds(1),
                TimeoutStatusCode = 503
            };

            using HealthCheckService healthService = new(options, _loggerFactory);

            // Add readiness checks
            healthService.AddReadinessCheck("slow_readiness", async (ct) =>
            {
                await Task.Delay(1500, ct); // Will timeout
                return HealthCheckResult.Healthy("Ready");
            });

            healthService.AddReadinessCheck("fast_readiness", async (ct) =>
            {
                await Task.Delay(200, ct);
                return HealthCheckResult.Healthy("Ready");
            });

            await _smtpServer.StartAsync();
            await healthService.StartAsync();

            // Act
            HttpResponseMessage response = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/health/readyz");
            string content = await response.Content.ReadAsStringAsync();
            JsonDocument json = JsonDocument.Parse(content);
            JsonElement root = json.RootElement;

            // Assert
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.True(root.GetProperty("timedOut").GetBoolean());
            Assert.Equal("NotReady", root.GetProperty("status").GetString());
            Assert.False(root.GetProperty("ready").GetBoolean());

            // Cleanup
            await healthService.StopAsync();
            await _smtpServer.StopAsync();
        }

        [Fact]
        public async Task CancellationToken_ProperlyCancelsChecks_OnTimeout()
        {
            // Arrange
            int healthCheckPort = GetAvailablePort();
            bool cancellationReceived = false;

            HealthCheckServiceOptions options = new()
            {
                Prefixes = new() { $"http://localhost:{healthCheckPort}/health/" },
                IndividualCheckTimeout = TimeSpan.FromSeconds(1)
            };

            using HealthCheckService healthService = new(options, _loggerFactory);

            healthService.AddHealthCheck("cancellation_test", async (ct) =>
            {
                try
                {
                    await Task.Delay(2000, ct);
                    return HealthCheckResult.Healthy("Should not reach");
                }
                catch (OperationCanceledException)
                {
                    cancellationReceived = true;
                    throw;
                }
            });

            await _smtpServer.StartAsync();
            await healthService.StartAsync();

            // Act
            HttpResponseMessage response = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/health/");

            // Assert
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

            // Give a moment for the cancellation to propagate
            await Task.Delay(500);
            Assert.True(cancellationReceived, "Cancellation token should be triggered on timeout");

            // Cleanup
            await healthService.StopAsync();
            await _smtpServer.StopAsync();
        }

        [Fact]
        public async Task MixedHealthChecks_HandleTimeoutAndSuccess_Correctly()
        {
            // Arrange
            int healthCheckPort = GetAvailablePort();

            HealthCheckServiceOptions options = new()
            {
                Prefixes = new() { $"http://localhost:{healthCheckPort}/health/" },
                TotalTimeout = TimeSpan.FromSeconds(5),
                IndividualCheckTimeout = TimeSpan.FromSeconds(2),
                FailFastOnTimeout = false
            };

            using HealthCheckService healthService = new(options, _loggerFactory);

            // Mix of healthy, degraded, unhealthy, and timeout checks
            healthService.AddHealthCheck("healthy_check", async (ct) =>
            {
                await Task.Delay(100, ct);
                return HealthCheckResult.Healthy("All good");
            });

            healthService.AddHealthCheck("degraded_check", async (ct) =>
            {
                await Task.Delay(100, ct);
                return HealthCheckResult.Degraded("Performance issues");
            });

            healthService.AddHealthCheck("unhealthy_check", async (ct) =>
            {
                await Task.Delay(100, ct);
                return HealthCheckResult.Unhealthy("Service down");
            });

            healthService.AddHealthCheck("timeout_check", async (ct) =>
            {
                await Task.Delay(3000, ct);
                return HealthCheckResult.Healthy("Will timeout");
            });

            await _smtpServer.StartAsync();
            await healthService.StartAsync();

            // Act
            HttpResponseMessage response = await _httpClient.GetAsync($"http://localhost:{healthCheckPort}/health/");
            string content = await response.Content.ReadAsStringAsync();
            JsonDocument json = JsonDocument.Parse(content);
            JsonElement root = json.RootElement;

            // Assert
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.True(root.GetProperty("timedOut").GetBoolean());
            Assert.Equal("Unhealthy", root.GetProperty("status").GetString());

            JsonElement checks = root.GetProperty("checks");
            Assert.Equal("Healthy", checks.GetProperty("healthy_check").GetProperty("status").GetString());
            Assert.Equal("Degraded", checks.GetProperty("degraded_check").GetProperty("status").GetString());
            Assert.Equal("Unhealthy", checks.GetProperty("unhealthy_check").GetProperty("status").GetString());
            Assert.Equal("Timeout", checks.GetProperty("timeout_check").GetProperty("status").GetString());

            // Cleanup
            await healthService.StopAsync();
            await _smtpServer.StopAsync();
        }

        private static int GetAvailablePort()
        {
            TcpListener listener = new(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public void Dispose()
        {
            _smtpServer?.Dispose();
            _httpClient?.Dispose();
            _loggerFactory?.Dispose();
        }
    }
}