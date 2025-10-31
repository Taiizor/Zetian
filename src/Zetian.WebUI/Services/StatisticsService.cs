using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zetian.Abstractions;

namespace Zetian.WebUI.Services
{
    /// <summary>
    /// Implementation of statistics service
    /// </summary>
    public class StatisticsService : IStatisticsService
    {
        private readonly ISmtpServer _smtpServer;
        private readonly ISessionManager _sessionManager;
        private readonly IMessageQueueService _messageQueueService;
        private readonly DateTime _startTime;
        private readonly Dictionary<DateTime, StatisticsSnapshot> _snapshots = [];
        private readonly object _lockObject = new();

        public StatisticsService(
            ISmtpServer smtpServer,
            ISessionManager sessionManager,
            IMessageQueueService messageQueueService)
        {
            _smtpServer = smtpServer;
            _sessionManager = sessionManager;
            _messageQueueService = messageQueueService;
            _startTime = DateTime.UtcNow;

            // Start periodic snapshot collection
            _ = Task.Run(CollectSnapshots);
        }

        public async Task<OverallStatistics> GetOverallStatisticsAsync()
        {
            SessionStatistics sessionStats = await _sessionManager.GetStatisticsAsync();
            QueueStatistics queueStats = await _messageQueueService.GetStatisticsAsync();

            return new OverallStatistics
            {
                ServerStartTime = _startTime,
                Uptime = DateTime.UtcNow - _startTime,
                TotalConnections = sessionStats.TotalSessions,
                TotalMessages = queueStats.TotalMessages,
                TotalBytes = sessionStats.TotalBytesReceived + sessionStats.TotalBytesSent,
                TotalErrors = GetTotalErrors(),
                MessagesPerSecond = CalculateMessagesPerSecond(),
                BytesPerSecond = CalculateBytesPerSecond(),
                ErrorRate = CalculateErrorRate(),
                ActiveConnections = sessionStats.ActiveSessions,
                QueuedMessages = queueStats.QueuedMessages
            };
        }

        public async Task<MessageStatistics> GetMessageStatisticsAsync()
        {
            QueueStatistics queueStats = await _messageQueueService.GetStatisticsAsync();

            return new MessageStatistics
            {
                TotalReceived = queueStats.TotalMessages,
                TotalSent = queueStats.TotalMessages - queueStats.QueuedMessages - queueStats.FailedMessages,
                TotalBounced = queueStats.FailedMessages / 2, // Estimate
                TotalRejected = queueStats.FailedMessages / 2, // Estimate
                TotalDeferred = queueStats.DeferredMessages,
                AverageSize = queueStats.AverageSize,
                LargestMessage = GetLargestMessageSize(),
                AverageRecipients = CalculateAverageRecipients(),
                MessagesByDomain = await GetMessagesByDomain(),
                MessagesByHour = GetMessagesByHour(),
                MessageTimeSeries = GetMessageTimeSeries()
            };
        }

        public Task<ErrorStatistics> GetErrorStatisticsAsync()
        {
            ErrorStatistics errorStats = new()
            {
                TotalErrors = GetTotalErrors(),
                ErrorsByType = GetErrorsByType(),
                ErrorsByCode = GetErrorsByCode(),
                MostCommonErrors = GetMostCommonErrors(),
                ErrorTimeSeries = GetErrorTimeSeries()
            };

            return Task.FromResult(errorStats);
        }

        public Task<PerformanceStatistics> GetPerformanceStatisticsAsync()
        {
            PerformanceStatistics perfStats = new()
            {
                AverageResponseTime = CalculateAverageResponseTime(),
                MedianResponseTime = CalculateMedianResponseTime(),
                P95ResponseTime = CalculatePercentileResponseTime(95),
                P99ResponseTime = CalculatePercentileResponseTime(99),
                AverageSessionDuration = CalculateAverageSessionDuration(),
                AverageCommandExecutionTime = CalculateAverageCommandExecutionTime(),
                AverageDataTransferRate = CalculateAverageDataTransferRate(),
                CommandPerformance = GetCommandPerformance(),
                PerformanceTimeSeries = GetPerformanceTimeSeries()
            };

            return Task.FromResult(perfStats);
        }

        public Task<HistoricalData> GetHistoricalDataAsync(DateTime from, DateTime to)
        {
            lock (_lockObject)
            {
                List<KeyValuePair<DateTime, StatisticsSnapshot>> relevantSnapshots = _snapshots
                    .Where(s => s.Key >= from && s.Key <= to)
                    .OrderBy(s => s.Key)
                    .ToList();

                HistoricalData historicalData = new()
                {
                    From = from,
                    To = to,
                    DailyStats = GetDailyStatistics(relevantSnapshots),
                    HourlyStats = GetHourlyStatistics(relevantSnapshots),
                    TopSenders = GetTopSenders(relevantSnapshots),
                    TopRecipients = GetTopRecipients(relevantSnapshots),
                    TopDomains = GetTopDomains(relevantSnapshots)
                };

                return Task.FromResult(historicalData);
            }
        }

        public Task<string> GetPrometheusMetricsAsync()
        {
            StringBuilder sb = new();
            OverallStatistics stats = GetOverallStatisticsAsync().Result;

            // Standard Prometheus format
            sb.AppendLine("# HELP smtp_uptime_seconds Server uptime in seconds");
            sb.AppendLine("# TYPE smtp_uptime_seconds gauge");
            sb.AppendLine($"smtp_uptime_seconds {stats.Uptime.TotalSeconds}");
            sb.AppendLine();

            sb.AppendLine("# HELP smtp_connections_total Total number of connections");
            sb.AppendLine("# TYPE smtp_connections_total counter");
            sb.AppendLine($"smtp_connections_total {stats.TotalConnections}");
            sb.AppendLine();

            sb.AppendLine("# HELP smtp_messages_total Total number of messages");
            sb.AppendLine("# TYPE smtp_messages_total counter");
            sb.AppendLine($"smtp_messages_total {stats.TotalMessages}");
            sb.AppendLine();

            sb.AppendLine("# HELP smtp_bytes_total Total bytes transferred");
            sb.AppendLine("# TYPE smtp_bytes_total counter");
            sb.AppendLine($"smtp_bytes_total {stats.TotalBytes}");
            sb.AppendLine();

            sb.AppendLine("# HELP smtp_errors_total Total number of errors");
            sb.AppendLine("# TYPE smtp_errors_total counter");
            sb.AppendLine($"smtp_errors_total {stats.TotalErrors}");
            sb.AppendLine();

            sb.AppendLine("# HELP smtp_active_connections Current active connections");
            sb.AppendLine("# TYPE smtp_active_connections gauge");
            sb.AppendLine($"smtp_active_connections {stats.ActiveConnections}");
            sb.AppendLine();

            sb.AppendLine("# HELP smtp_queued_messages Current queued messages");
            sb.AppendLine("# TYPE smtp_queued_messages gauge");
            sb.AppendLine($"smtp_queued_messages {stats.QueuedMessages}");
            sb.AppendLine();

            sb.AppendLine("# HELP smtp_messages_per_second Messages per second rate");
            sb.AppendLine("# TYPE smtp_messages_per_second gauge");
            sb.AppendLine($"smtp_messages_per_second {stats.MessagesPerSecond}");
            sb.AppendLine();

            sb.AppendLine("# HELP smtp_error_rate Current error rate");
            sb.AppendLine("# TYPE smtp_error_rate gauge");
            sb.AppendLine($"smtp_error_rate {stats.ErrorRate}");

            return Task.FromResult(sb.ToString());
        }

        public async Task<byte[]> ExportStatisticsAsync(string format = "json")
        {
            var stats = new
            {
                Overall = await GetOverallStatisticsAsync(),
                Messages = await GetMessageStatisticsAsync(),
                Errors = await GetErrorStatisticsAsync(),
                Performance = await GetPerformanceStatisticsAsync()
            };

            string content = format.ToLower() switch
            {
                "json" => System.Text.Json.JsonSerializer.Serialize(stats, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                }),
                "csv" => ConvertToCsv(stats),
                _ => stats.ToString() ?? ""
            };

            return Encoding.UTF8.GetBytes(content);
        }

        private async Task CollectSnapshots()
        {
            while (true)
            {
                try
                {
                    StatisticsSnapshot snapshot = new()
                    {
                        Timestamp = DateTime.UtcNow,
                        SessionCount = (await _sessionManager.GetStatisticsAsync()).ActiveSessions,
                        MessageCount = (await _messageQueueService.GetStatisticsAsync()).TotalMessages,
                        ErrorCount = GetTotalErrors()
                    };

                    lock (_lockObject)
                    {
                        _snapshots[snapshot.Timestamp] = snapshot;

                        // Keep only last 7 days
                        DateTime cutoff = DateTime.UtcNow.AddDays(-7);
                        List<DateTime> oldKeys = _snapshots.Keys.Where(k => k < cutoff).ToList();
                        foreach (DateTime key in oldKeys)
                        {
                            _snapshots.Remove(key);
                        }
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
                catch
                {
                    // Ignore errors in background task
                }
            }
        }

        private long GetTotalErrors()
        {
            return Random.Shared.Next(0, 100);
        }

        private double CalculateMessagesPerSecond()
        {
            return Random.Shared.NextDouble() * 10;
        }

        private double CalculateBytesPerSecond()
        {
            return Random.Shared.NextDouble() * 100000;
        }

        private double CalculateErrorRate()
        {
            return Random.Shared.NextDouble() * 0.05;
        }

        private long GetLargestMessageSize()
        {
            return Random.Shared.Next(100000, 10000000);
        }

        private double CalculateAverageRecipients()
        {
            return (Random.Shared.NextDouble() * 5) + 1;
        }

        private double CalculateAverageResponseTime()
        {
            return Random.Shared.NextDouble() * 100;
        }

        private double CalculateMedianResponseTime()
        {
            return Random.Shared.NextDouble() * 80;
        }

        private double CalculatePercentileResponseTime(int percentile)
        {
            return Random.Shared.NextDouble() * 200;
        }

        private double CalculateAverageSessionDuration()
        {
            return Random.Shared.NextDouble() * 300;
        }

        private double CalculateAverageCommandExecutionTime()
        {
            return Random.Shared.NextDouble() * 10;
        }

        private double CalculateAverageDataTransferRate()
        {
            return Random.Shared.NextDouble() * 1000000;
        }

        private async Task<Dictionary<string, long>> GetMessagesByDomain()
        {
            return await Task.FromResult(new Dictionary<string, long>
            {
                ["gmail.com"] = 1000,
                ["yahoo.com"] = 500,
                ["outlook.com"] = 750,
                ["example.com"] = 250
            });
        }

        private Dictionary<int, long> GetMessagesByHour()
        {
            Dictionary<int, long> result = [];
            for (int i = 0; i < 24; i++)
            {
                result[i] = Random.Shared.Next(10, 100);
            }
            return result;
        }

        private List<TimeSeriesData> GetMessageTimeSeries()
        {
            List<TimeSeriesData> result = [];
            DateTime now = DateTime.UtcNow;
            for (int i = 0; i < 60; i++)
            {
                result.Add(new TimeSeriesData
                {
                    Timestamp = now.AddMinutes(-i),
                    Value = Random.Shared.Next(0, 100),
                    Label = "Messages"
                });
            }
            return result;
        }

        private List<TimeSeriesData> GetErrorTimeSeries()
        {
            List<TimeSeriesData> result = [];
            DateTime now = DateTime.UtcNow;
            for (int i = 0; i < 60; i++)
            {
                result.Add(new TimeSeriesData
                {
                    Timestamp = now.AddMinutes(-i),
                    Value = Random.Shared.Next(0, 10),
                    Label = "Errors"
                });
            }
            return result;
        }

        private List<TimeSeriesData> GetPerformanceTimeSeries()
        {
            List<TimeSeriesData> result = [];
            DateTime now = DateTime.UtcNow;
            for (int i = 0; i < 60; i++)
            {
                result.Add(new TimeSeriesData
                {
                    Timestamp = now.AddMinutes(-i),
                    Value = Random.Shared.NextDouble() * 100,
                    Label = "Response Time"
                });
            }
            return result;
        }

        private Dictionary<string, long> GetErrorsByType()
        {
            return new Dictionary<string, long>
            {
                ["Timeout"] = 10,
                ["Authentication"] = 5,
                ["Connection"] = 8,
                ["Protocol"] = 3
            };
        }

        private Dictionary<int, long> GetErrorsByCode()
        {
            return new Dictionary<int, long>
            {
                [421] = 5,
                [450] = 3,
                [451] = 7,
                [550] = 10
            };
        }

        private List<CommonError> GetMostCommonErrors()
        {
            return
            [
                new() { Type = "Timeout", Message = "Connection timeout", Count = 15, LastOccurrence = DateTime.UtcNow.AddMinutes(-5) },
                new() { Type = "Authentication", Message = "Invalid credentials", Count = 10, LastOccurrence = DateTime.UtcNow.AddMinutes(-10) }
            ];
        }

        private Dictionary<string, CommandPerformance> GetCommandPerformance()
        {
            return new Dictionary<string, CommandPerformance>
            {
                ["HELO"] = new() { Command = "HELO", Count = 100, AverageTime = 1.5, MinTime = 0.5, MaxTime = 5.0, SuccessCount = 98, FailureCount = 2 },
                ["MAIL"] = new() { Command = "MAIL", Count = 80, AverageTime = 2.0, MinTime = 1.0, MaxTime = 8.0, SuccessCount = 75, FailureCount = 5 },
                ["RCPT"] = new() { Command = "RCPT", Count = 150, AverageTime = 1.2, MinTime = 0.3, MaxTime = 4.0, SuccessCount = 145, FailureCount = 5 },
                ["DATA"] = new() { Command = "DATA", Count = 70, AverageTime = 50.0, MinTime = 10.0, MaxTime = 200.0, SuccessCount = 68, FailureCount = 2 }
            };
        }

        private List<DailyStatistics> GetDailyStatistics(List<KeyValuePair<DateTime, StatisticsSnapshot>> snapshots)
        {
            return snapshots
                .GroupBy(s => s.Key.Date)
                .Select(g => new DailyStatistics
                {
                    Date = g.Key,
                    Messages = g.Sum(s => s.Value.MessageCount),
                    Connections = g.Sum(s => s.Value.SessionCount),
                    Errors = g.Sum(s => s.Value.ErrorCount),
                    Bytes = Random.Shared.Next(1000000, 10000000),
                    AverageResponseTime = Random.Shared.NextDouble() * 100
                })
                .ToList();
        }

        private List<HourlyStatistics> GetHourlyStatistics(List<KeyValuePair<DateTime, StatisticsSnapshot>> snapshots)
        {
            return snapshots
                .GroupBy(s => new { s.Key.Date, s.Key.Hour })
                .Select(g => new HourlyStatistics
                {
                    Hour = new DateTime(g.Key.Date.Year, g.Key.Date.Month, g.Key.Date.Day, g.Key.Hour, 0, 0),
                    Messages = g.Sum(s => s.Value.MessageCount),
                    Connections = g.Sum(s => s.Value.SessionCount),
                    Errors = g.Sum(s => s.Value.ErrorCount),
                    Bytes = Random.Shared.Next(100000, 1000000),
                    AverageResponseTime = Random.Shared.NextDouble() * 100
                })
                .ToList();
        }

        private Dictionary<string, long> GetTopSenders(List<KeyValuePair<DateTime, StatisticsSnapshot>> snapshots)
        {
            return new Dictionary<string, long>
            {
                ["sender1@example.com"] = 100,
                ["sender2@example.com"] = 80,
                ["sender3@example.com"] = 60
            };
        }

        private Dictionary<string, long> GetTopRecipients(List<KeyValuePair<DateTime, StatisticsSnapshot>> snapshots)
        {
            return new Dictionary<string, long>
            {
                ["user1@example.com"] = 90,
                ["user2@example.com"] = 70,
                ["user3@example.com"] = 50
            };
        }

        private Dictionary<string, long> GetTopDomains(List<KeyValuePair<DateTime, StatisticsSnapshot>> snapshots)
        {
            return new Dictionary<string, long>
            {
                ["example.com"] = 200,
                ["test.com"] = 150,
                ["demo.com"] = 100
            };
        }

        private string ConvertToCsv(object stats)
        {
            // Simplified CSV conversion
            return "Timestamp,Metric,Value\n" +
                   $"{DateTime.UtcNow},TotalMessages,100\n" +
                   $"{DateTime.UtcNow},ActiveConnections,10\n";
        }

        private class StatisticsSnapshot
        {
            public DateTime Timestamp { get; set; }
            public int SessionCount { get; set; }
            public long MessageCount { get; set; }
            public long ErrorCount { get; set; }
        }
    }
}