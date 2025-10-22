using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.HealthCheck.Abstractions;
using Zetian.HealthCheck.Models;
using Zetian.HealthCheck.Options;
using Zetian.Server;

namespace Zetian.HealthCheck.Checks
{
    /// <summary>
    /// Health check for SMTP server
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of SmtpServerHealthCheck
    /// </remarks>
    public class SmtpServerHealthCheck(ISmtpServer server, SmtpHealthCheckOptions? options = null) : IHealthCheck
    {
        private readonly ISmtpServer _server = server ?? throw new ArgumentNullException(nameof(server));
        private readonly SmtpHealthCheckOptions _options = options ?? new SmtpHealthCheckOptions();

        /// <inheritdoc />
        public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Dictionary<string, object> data = new()
                {
                    ["status"] = _server.IsRunning ? "running" : "stopped",
                    ["uptime"] = GetUptime(),
                    ["endpoint"] = _server.Endpoint?.ToString() ?? "not bound",
                    ["configuration"] = new Dictionary<string, object>
                    {
                        ["serverName"] = _server.Configuration.ServerName,
                        ["maxConnections"] = _server.Configuration.MaxConnections,
                        ["maxMessageSize"] = _server.Configuration.MaxMessageSize,
                        ["requireAuthentication"] = _server.Configuration.RequireAuthentication,
                        ["requireSecureConnection"] = _server.Configuration.RequireSecureConnection
                    }
                };

                // Check active sessions if available
                if (_server is SmtpServer smtpServer)
                {
                    int sessionCount = GetActiveSessionCount(smtpServer);
                    data["activeSessions"] = sessionCount;
                    data["maxSessions"] = _server.Configuration.MaxConnections;

                    double utilizationPercent = (double)sessionCount / _server.Configuration.MaxConnections * 100;
                    data["utilizationPercent"] = Math.Round(utilizationPercent, 2);

                    // Check if server is running
                    if (!_server.IsRunning)
                    {
                        return Task.FromResult(HealthCheckResult.Unhealthy("SMTP server is not running", data: data));
                    }

                    // Check utilization thresholds
                    if (utilizationPercent >= _options.UnhealthyThresholdPercent)
                    {
                        return Task.FromResult(HealthCheckResult.Unhealthy(
                            $"Server utilization is too high: {utilizationPercent:F2}%",
                            data: data));
                    }

                    if (utilizationPercent >= _options.DegradedThresholdPercent)
                    {
                        return Task.FromResult(HealthCheckResult.Degraded(
                            $"Server utilization is elevated: {utilizationPercent:F2}%",
                            data: data));
                    }
                }
                else if (!_server.IsRunning)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy("SMTP server is not running", data: data));
                }

                // Add memory usage if enabled
                if (_options.CheckMemoryUsage)
                {
                    Dictionary<string, object> memoryInfo = GetMemoryInfo();
                    data["memory"] = memoryInfo;
                }

                return Task.FromResult(HealthCheckResult.Healthy("SMTP server is healthy", data: data));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Error checking SMTP server health", ex));
            }
        }

        private int GetActiveSessionCount(SmtpServer server)
        {
            // Use reflection to access private field (for health monitoring purposes)
            FieldInfo? sessionsField = typeof(SmtpServer).GetField("_sessions",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (sessionsField?.GetValue(server) is ConcurrentDictionary<string, object> sessions)
            {
                return sessions.Count;
            }

            return 0;
        }

        private string GetUptime()
        {
            if (_server is SmtpServer smtpServer && smtpServer.StartTime.HasValue)
            {
                TimeSpan uptime = DateTime.UtcNow - smtpServer.StartTime.Value;
                return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
            }
            return "unknown";
        }

        private Dictionary<string, object> GetMemoryInfo()
        {
            Process process = Process.GetCurrentProcess();
            return new Dictionary<string, object>
            {
                ["workingSet"] = process.WorkingSet64,
                ["privateMemory"] = process.PrivateMemorySize64,
                ["virtualMemory"] = process.VirtualMemorySize64,
                ["gcTotalMemory"] = GC.GetTotalMemory(false)
            };
        }
    }
}