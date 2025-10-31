using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;

namespace Zetian.WebUI.Services
{
    /// <summary>
    /// Implementation of log service
    /// </summary>
    public class LogService : ILogService
    {
        private readonly ISmtpServer _smtpServer;
        private readonly ConcurrentQueue<LogEntry> _logs = new();
        private readonly SemaphoreSlim _logSemaphore = new(1, 1);
        private long _logIdCounter = 0;

        public LogService(ISmtpServer smtpServer)
        {
            _smtpServer = smtpServer;

            // Subscribe to server events for logging
            SubscribeToEvents();
        }

        public Task<IEnumerable<LogEntry>> GetRecentLogsAsync(int count = 100, string? filter = null, string? level = null)
        {
            IEnumerable<LogEntry> query = _logs.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(l =>
                    l.Message.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    l.Category.Contains(filter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(level) && Enum.TryParse<LogLevel>(level, true, out LogLevel logLevel))
            {
                query = query.Where(l => l.Level >= logLevel);
            }

            List<LogEntry> result = query
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToList();

            return Task.FromResult(result.AsEnumerable());
        }

        public Task<PagedResult<LogEntry>> GetPagedLogsAsync(int page, int pageSize, LogFilter? filter = null)
        {
            IEnumerable<LogEntry> query = ApplyFilter(_logs.AsEnumerable(), filter);
            int totalItems = query.Count();

            List<LogEntry> items = query
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Task.FromResult(new PagedResult<LogEntry>
            {
                Items = items,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            });
        }

        public Task<IEnumerable<LogEntry>> SearchLogsAsync(string query, DateTime? from = null, DateTime? to = null)
        {
            IEnumerable<LogEntry> logs = _logs.AsEnumerable();

            if (from.HasValue)
            {
                logs = logs.Where(l => l.Timestamp >= from.Value);
            }

            if (to.HasValue)
            {
                logs = logs.Where(l => l.Timestamp <= to.Value);
            }

            logs = logs.Where(l =>
                l.Message.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                l.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (l.Exception?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));

            return Task.FromResult(logs.OrderByDescending(l => l.Timestamp).AsEnumerable());
        }

        public Task<byte[]> ExportLogsAsync(DateTime from, DateTime to, string format = "txt")
        {
            List<LogEntry> logs = _logs
                .Where(l => l.Timestamp >= from && l.Timestamp <= to)
                .OrderBy(l => l.Timestamp)
                .ToList();

            string content = format.ToLower() switch
            {
                "json" => System.Text.Json.JsonSerializer.Serialize(logs, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                }),
                "csv" => ExportToCsv(logs),
                _ => ExportToText(logs)
            };

            return Task.FromResult(Encoding.UTF8.GetBytes(content));
        }

        public Task<int> ClearOldLogsAsync(DateTime before)
        {
            int count = 0;
            List<LogEntry> logsToKeep = [];

            while (_logs.TryDequeue(out LogEntry? log))
            {
                if (log.Timestamp >= before)
                {
                    logsToKeep.Add(log);
                }
                else
                {
                    count++;
                }
            }

            foreach (LogEntry log in logsToKeep)
            {
                _logs.Enqueue(log);
            }

            return Task.FromResult(count);
        }

        public Task<LogStatistics> GetStatisticsAsync()
        {
            List<LogEntry> logs = _logs.ToList();
            DateTime now = DateTime.UtcNow;
            List<LogTrend> trends = [];

            for (int i = 0; i < 24; i++)
            {
                DateTime hour = now.AddHours(-i);
                List<LogEntry> hourLogs = logs.Where(l =>
                    l.Timestamp >= hour && l.Timestamp < hour.AddHours(1)).ToList();

                trends.Add(new LogTrend
                {
                    Timestamp = hour,
                    CountByLevel = hourLogs
                        .GroupBy(l => l.Level)
                        .ToDictionary(g => g.Key, g => g.Count())
                });
            }

            return Task.FromResult(new LogStatistics
            {
                TotalEntries = logs.Count,
                EntriesByLevel = logs.GroupBy(l => l.Level)
                    .ToDictionary(g => g.Key, g => (long)g.Count()),
                EntriesByCategory = logs.GroupBy(l => l.Category)
                    .ToDictionary(g => g.Key, g => (long)g.Count()),
                ErrorCount = logs.Count(l => l.Level >= LogLevel.Error),
                WarningCount = logs.Count(l => l.Level == LogLevel.Warning),
                OldestEntry = logs.Any() ? logs.Min(l => l.Timestamp) : DateTime.UtcNow,
                NewestEntry = logs.Any() ? logs.Max(l => l.Timestamp) : DateTime.UtcNow,
                EntriesPerSecond = CalculateEntriesPerSecond(logs),
                Trends = trends
            });
        }

        public async IAsyncEnumerable<LogEntry> SubscribeToLogsAsync(
            LogFilter? filter = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            long lastId = _logs.Any() ? _logs.Max(l => l.Id) : 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                await _logSemaphore.WaitAsync(cancellationToken);
                try
                {
                    List<LogEntry> newLogs = _logs
                        .Where(l => l.Id > lastId)
                        .ToList();

                    if (newLogs.Any())
                    {
                        lastId = newLogs.Max(l => l.Id);

                        foreach (LogEntry log in ApplyFilter(newLogs, filter))
                        {
                            yield return log;
                        }
                    }
                }
                finally
                {
                    _logSemaphore.Release();
                }

                await Task.Delay(100, cancellationToken);
            }
        }

        private void SubscribeToEvents()
        {
            _smtpServer.SessionCreated += (sender, e) =>
                AddLog(LogLevel.Information, "Session", $"Session created: {e.Session.Id}", e.Session.Id);

            _smtpServer.SessionCompleted += (sender, e) =>
                AddLog(LogLevel.Information, "Session", $"Session completed: {e.Session.Id}", e.Session.Id);

            _smtpServer.MessageReceived += (sender, e) =>
                AddLog(LogLevel.Information, "Message", $"Message received from {e.Message.From}", e.Session.Id);

            _smtpServer.ErrorOccurred += (sender, e) =>
                AddLog(LogLevel.Error, "Error", e.Exception.Message, exception: e.Exception.ToString());

            _smtpServer.AuthenticationSucceeded += (sender, e) =>
                AddLog(LogLevel.Information, "Authentication", $"User {e.AuthenticatedIdentity} authenticated", e.Session.Id);

            _smtpServer.AuthenticationFailed += (sender, e) =>
                AddLog(LogLevel.Warning, "Authentication", $"Authentication failed for {e.Username}", e.Session.Id);

            _smtpServer.CommandReceived += (sender, e) =>
                AddLog(LogLevel.Debug, "Command", $"Command: {e.Command.Verb} {e.Command.Parameters}", e.Session.Id);
        }

        private void AddLog(LogLevel level, string category, string message, string? sessionId = null, string? exception = null)
        {
            LogEntry logEntry = new()
            {
                Id = Interlocked.Increment(ref _logIdCounter),
                Timestamp = DateTime.UtcNow,
                Level = level,
                Category = category,
                Message = message,
                SessionId = sessionId,
                Exception = exception
            };

            _logs.Enqueue(logEntry);

            // Keep only last 10000 logs
            while (_logs.Count > 10000)
            {
                _logs.TryDequeue(out _);
            }

            _logSemaphore.Release();
        }

        private IEnumerable<LogEntry> ApplyFilter(IEnumerable<LogEntry> logs, LogFilter? filter)
        {
            if (filter == null)
            {
                return logs;
            }

            IEnumerable<LogEntry> query = logs.AsEnumerable();

            if (filter.MinLevel.HasValue)
            {
                query = query.Where(l => l.Level >= filter.MinLevel.Value);
            }

            if (filter.MaxLevel.HasValue)
            {
                query = query.Where(l => l.Level <= filter.MaxLevel.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Category))
            {
                query = query.Where(l => l.Category.Contains(filter.Category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filter.SessionId))
            {
                query = query.Where(l => l.SessionId == filter.SessionId);
            }

            if (!string.IsNullOrWhiteSpace(filter.RemoteIp))
            {
                query = query.Where(l => l.RemoteIp == filter.RemoteIp);
            }

            if (!string.IsNullOrWhiteSpace(filter.User))
            {
                query = query.Where(l => l.User == filter.User);
            }

            if (filter.From.HasValue)
            {
                query = query.Where(l => l.Timestamp >= filter.From.Value);
            }

            if (filter.To.HasValue)
            {
                query = query.Where(l => l.Timestamp <= filter.To.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                query = query.Where(l =>
                    l.Message.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase) ||
                    l.Category.Contains(filter.SearchText, StringComparison.OrdinalIgnoreCase));
            }

            if (!filter.IncludeExceptions)
            {
                query = query.Where(l => string.IsNullOrEmpty(l.Exception));
            }

            return query;
        }

        private double CalculateEntriesPerSecond(List<LogEntry> logs)
        {
            if (logs.Count < 2)
            {
                return 0;
            }

            List<LogEntry> recentLogs = logs.Where(l => l.Timestamp > DateTime.UtcNow.AddMinutes(-1)).ToList();
            return recentLogs.Count / 60.0;
        }

        private string ExportToText(List<LogEntry> logs)
        {
            StringBuilder sb = new();
            foreach (LogEntry log in logs)
            {
                sb.AppendLine($"[{log.Timestamp:yyyy-MM-dd HH:mm:ss}] [{log.Level}] [{log.Category}] {log.Message}");
                if (!string.IsNullOrEmpty(log.Exception))
                {
                    sb.AppendLine($"  Exception: {log.Exception}");
                }
            }
            return sb.ToString();
        }

        private string ExportToCsv(List<LogEntry> logs)
        {
            StringBuilder sb = new();
            sb.AppendLine("Timestamp,Level,Category,Message,SessionId,Exception");

            foreach (LogEntry log in logs)
            {
                sb.AppendLine($"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.Level}\",\"{log.Category}\",\"{log.Message}\",\"{log.SessionId}\",\"{log.Exception}\"");
            }

            return sb.ToString();
        }
    }
}