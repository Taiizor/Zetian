using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Zetian.Abstractions;

namespace Zetian.WebUI.Services
{
    /// <summary>
    /// Implementation of dashboard service
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly ISmtpServer _smtpServer;
        private readonly ISessionManager _sessionManager;
        private readonly IMessageQueueService _messageQueueService;
        private readonly DateTime _startTime;
        private readonly List<ActivityItem> _recentActivity = [];
        private readonly object _lockObject = new();

        public DashboardService(
            ISmtpServer smtpServer,
            ISessionManager sessionManager,
            IMessageQueueService messageQueueService)
        {
            _smtpServer = smtpServer;
            _sessionManager = sessionManager;
            _messageQueueService = messageQueueService;
            _startTime = DateTime.UtcNow;

            // Subscribe to server events
            SubscribeToEvents();
        }

        public async Task<DashboardOverview> GetOverviewAsync()
        {
            SessionStatistics sessionStats = await _sessionManager.GetStatisticsAsync();
            QueueStatistics queueStats = await _messageQueueService.GetStatisticsAsync();
            Process process = Process.GetCurrentProcess();

            return new DashboardOverview
            {
                ServerStatus = _smtpServer.IsRunning ? ServerStatus.Running : ServerStatus.Stopped,
                TotalMessages = queueStats.TotalMessages,
                ActiveSessions = sessionStats.ActiveSessions,
                TotalErrors = GetTotalErrors(),
                MessagesPerSecond = CalculateMessagesPerSecond(),
                Uptime = DateTime.UtcNow - _startTime,
                CpuUsage = GetCpuUsage(),
                MemoryUsage = process.WorkingSet64,
                NetworkThroughput = CalculateNetworkThroughput(),
                LastUpdate = DateTime.UtcNow
            };
        }

        public async Task<ServerMetrics> GetMetricsAsync()
        {
            SessionStatistics sessionStats = await _sessionManager.GetStatisticsAsync();
            QueueStatistics queueStats = await _messageQueueService.GetStatisticsAsync();

            return new ServerMetrics
            {
                MessagesReceived = queueStats.TotalMessages,
                MessagesSent = queueStats.TotalMessages - queueStats.QueuedMessages,
                MessagesQueued = queueStats.QueuedMessages,
                MessagesFailed = queueStats.FailedMessages,
                SessionsCreated = sessionStats.TotalSessions,
                SessionsCompleted = sessionStats.TotalSessions - sessionStats.ActiveSessions,
                AuthenticationAttempts = GetAuthAttempts(),
                AuthenticationSuccesses = GetAuthSuccesses(),
                AuthenticationFailures = GetAuthFailures(),
                TlsNegotiations = GetTlsNegotiations(),
                AverageMessageSize = queueStats.AverageSize,
                AverageSessionDuration = sessionStats.AverageDuration
            };
        }

        public Task<IEnumerable<ActivityItem>> GetRecentActivityAsync(int count = 50)
        {
            lock (_lockObject)
            {
                return Task.FromResult(_recentActivity
                    .OrderByDescending(a => a.Timestamp)
                    .Take(count)
                    .AsEnumerable());
            }
        }

        public async Task<HealthStatus> GetHealthStatusAsync()
        {
            Dictionary<string, ComponentHealth> components = new()
            {
                // Check SMTP server
                ["SmtpServer"] = new ComponentHealth
                {
                    Name = "SMTP Server",
                    IsHealthy = _smtpServer.IsRunning,
                    Status = _smtpServer.IsRunning ? "Running" : "Stopped"
                }
            };

            // Check session manager
            SessionStatistics sessionStats = await _sessionManager.GetStatisticsAsync();
            components["SessionManager"] = new ComponentHealth
            {
                Name = "Session Manager",
                IsHealthy = true,
                Status = $"{sessionStats.ActiveSessions} active sessions"
            };

            // Check message queue
            QueueStatistics queueStats = await _messageQueueService.GetStatisticsAsync();
            bool queueHealthy = queueStats.QueuedMessages < 1000; // Example threshold
            components["MessageQueue"] = new ComponentHealth
            {
                Name = "Message Queue",
                IsHealthy = queueHealthy,
                Status = $"{queueStats.QueuedMessages} messages in queue",
                Message = queueHealthy ? null : "Queue is getting full"
            };

            // Check memory
            Process process = Process.GetCurrentProcess();
            bool memoryHealthy = process.WorkingSet64 < 2L * 1024 * 1024 * 1024; // 2GB threshold
            components["Memory"] = new ComponentHealth
            {
                Name = "Memory",
                IsHealthy = memoryHealthy,
                Status = $"{process.WorkingSet64 / (1024 * 1024)} MB",
                Message = memoryHealthy ? null : "High memory usage"
            };

            return new HealthStatus
            {
                IsHealthy = components.Values.All(c => c.IsHealthy),
                Components = components,
                CheckTime = DateTime.UtcNow
            };
        }

        public Task<PerformanceData> GetPerformanceDataAsync(DateTime from, DateTime to)
        {
            // Generate sample performance data
            PerformanceData data = new();
            TimeSpan interval = TimeSpan.FromMinutes(1);
            DateTime current = from;

            while (current <= to)
            {
                data.MessagesPerMinute.Add(new DataPoint
                {
                    Timestamp = current,
                    Value = Random.Shared.Next(0, 100)
                });

                data.SessionsPerMinute.Add(new DataPoint
                {
                    Timestamp = current,
                    Value = Random.Shared.Next(0, 20)
                });

                data.ErrorsPerMinute.Add(new DataPoint
                {
                    Timestamp = current,
                    Value = Random.Shared.Next(0, 5)
                });

                data.CpuUsage.Add(new DataPoint
                {
                    Timestamp = current,
                    Value = Random.Shared.Next(10, 80)
                });

                data.MemoryUsage.Add(new DataPoint
                {
                    Timestamp = current,
                    Value = Random.Shared.Next(200, 500)
                });

                data.NetworkThroughput.Add(new DataPoint
                {
                    Timestamp = current,
                    Value = Random.Shared.Next(1000, 10000)
                });

                current = current.Add(interval);
            }

            return Task.FromResult(data);
        }

        public Task<IEnumerable<TopSender>> GetTopSendersAsync(int count = 10)
        {
            // Return sample data - in production, this would query actual data
            List<TopSender> senders = [];
            for (int i = 0; i < count; i++)
            {
                senders.Add(new TopSender
                {
                    Address = $"sender{i}@example.com",
                    MessageCount = Random.Shared.Next(10, 100),
                    TotalSize = Random.Shared.Next(10000, 1000000),
                    LastSeen = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(1, 60))
                });
            }

            return Task.FromResult(senders.OrderByDescending(s => s.MessageCount).AsEnumerable());
        }

        public Task<IEnumerable<TopRecipient>> GetTopRecipientsAsync(int count = 10)
        {
            // Return sample data - in production, this would query actual data
            List<TopRecipient> recipients = [];
            for (int i = 0; i < count; i++)
            {
                recipients.Add(new TopRecipient
                {
                    Address = $"recipient{i}@example.com",
                    MessageCount = Random.Shared.Next(10, 100),
                    TotalSize = Random.Shared.Next(10000, 1000000),
                    LastSeen = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(1, 60))
                });
            }

            return Task.FromResult(recipients.OrderByDescending(r => r.MessageCount).AsEnumerable());
        }

        public Task<ErrorSummary> GetErrorSummaryAsync()
        {
            return Task.FromResult(new ErrorSummary
            {
                TotalErrors = GetTotalErrors(),
                ErrorsByType = new Dictionary<string, long>
                {
                    ["Authentication"] = 10,
                    ["Connection"] = 5,
                    ["Timeout"] = 3,
                    ["Invalid Data"] = 7
                },
                RecentErrors = _recentActivity
                    .Where(a => a.Level == ActivityLevel.Error)
                    .Select(a => new RecentError
                    {
                        Timestamp = a.Timestamp,
                        Type = "General",
                        Message = a.Description
                    })
                    .Take(10)
                    .ToList()
            });
        }

        private void SubscribeToEvents()
        {
            _smtpServer.SessionCreated += (sender, e) =>
            {
                AddActivity("Session Created", $"New session from {e.Session.RemoteEndPoint}", ActivityLevel.Info);
            };

            _smtpServer.MessageReceived += (sender, e) =>
            {
                AddActivity("Message Received", $"From: {e.Message.From}, Size: {e.Message.Size}", ActivityLevel.Success);
            };

            _smtpServer.ErrorOccurred += (sender, e) =>
            {
                AddActivity("Error", e.Exception.Message, ActivityLevel.Error);
            };

            _smtpServer.AuthenticationSucceeded += (sender, e) =>
            {
                AddActivity("Authentication", $"User {e.AuthenticatedIdentity} logged in", ActivityLevel.Success);
            };

            _smtpServer.AuthenticationFailed += (sender, e) =>
            {
                AddActivity("Authentication Failed", $"Failed attempt for {e.Username}", ActivityLevel.Warning);
            };
        }

        private void AddActivity(string type, string description, ActivityLevel level)
        {
            lock (_lockObject)
            {
                _recentActivity.Add(new ActivityItem
                {
                    Timestamp = DateTime.UtcNow,
                    Type = type,
                    Description = description,
                    Level = level
                });

                // Keep only last 1000 items
                while (_recentActivity.Count > 1000)
                {
                    _recentActivity.RemoveAt(0);
                }
            }
        }

        private long GetTotalErrors()
        {
            return _recentActivity.Count(a => a.Level == ActivityLevel.Error);
        }

        private double CalculateMessagesPerSecond()
        {
            return _recentActivity.Count(a => a.Type == "Message Received" && a.Timestamp > DateTime.UtcNow.AddSeconds(-60)) / 60.0;
        }

        private double GetCpuUsage()
        {
            return Process.GetCurrentProcess().TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount / Environment.TickCount * 100;
        }

        private double CalculateNetworkThroughput()
        {
            return Random.Shared.Next(1000, 10000); // Sample data
        }

        private int GetAuthAttempts()
        {
            return _recentActivity.Count(a => a.Type.Contains("Authentication"));
        }

        private int GetAuthSuccesses()
        {
            return _recentActivity.Count(a => a.Type == "Authentication" && a.Level == ActivityLevel.Success);
        }

        private int GetAuthFailures()
        {
            return _recentActivity.Count(a => a.Type == "Authentication Failed");
        }

        private int GetTlsNegotiations()
        {
            return _recentActivity.Count(a => a.Type.Contains("TLS"));
        }
    }
}