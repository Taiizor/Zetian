using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.Models.EventArgs;

namespace Zetian.WebUI.Services
{
    /// <summary>
    /// Implementation of message queue service
    /// </summary>
    public class MessageQueueService : IMessageQueueService
    {
        private readonly ISmtpServer _smtpServer;
        private readonly ConcurrentDictionary<string, QueuedMessage> _messages = new();
        private readonly object _lockObject = new();

        public MessageQueueService(ISmtpServer smtpServer)
        {
            _smtpServer = smtpServer;

            // Subscribe to server events
            SubscribeToEvents();
        }

        public Task<IEnumerable<QueuedMessage>> GetQueuedMessagesAsync()
        {
            return Task.FromResult(_messages.Values
                .Where(m => m.Status == MessageStatus.Queued)
                .OrderBy(m => m.ReceivedTime)
                .AsEnumerable());
        }

        public Task<PagedResult<QueuedMessage>> GetPagedMessagesAsync(int page, int pageSize)
        {
            List<QueuedMessage> allMessages = _messages.Values.OrderByDescending(m => m.ReceivedTime).ToList();
            int totalItems = allMessages.Count;
            List<QueuedMessage> items = allMessages
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Task.FromResult(new PagedResult<QueuedMessage>
            {
                Items = items,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            });
        }

        public Task<QueuedMessage?> GetMessageAsync(string messageId)
        {
            _messages.TryGetValue(messageId, out QueuedMessage? message);
            return Task.FromResult(message);
        }

        public Task<IEnumerable<QueuedMessage>> SearchMessagesAsync(MessageSearchCriteria criteria)
        {
            IEnumerable<QueuedMessage> query = _messages.Values.AsEnumerable();

            if (!string.IsNullOrEmpty(criteria.From))
            {
                query = query.Where(m => m.From.Contains(criteria.From, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(criteria.To))
            {
                query = query.Where(m => m.To.Any(t => t.Contains(criteria.To, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrEmpty(criteria.Subject))
            {
                query = query.Where(m => m.Subject?.Contains(criteria.Subject, StringComparison.OrdinalIgnoreCase) ?? false);
            }

            if (criteria.FromDate.HasValue)
            {
                query = query.Where(m => m.ReceivedTime >= criteria.FromDate.Value);
            }

            if (criteria.ToDate.HasValue)
            {
                query = query.Where(m => m.ReceivedTime <= criteria.ToDate.Value);
            }

            if (criteria.Status.HasValue)
            {
                query = query.Where(m => m.Status == criteria.Status.Value);
            }

            if (criteria.Priority.HasValue)
            {
                query = query.Where(m => m.Priority == criteria.Priority.Value);
            }

            if (!string.IsNullOrEmpty(criteria.SessionId))
            {
                query = query.Where(m => m.SessionId == criteria.SessionId);
            }

            if (!string.IsNullOrEmpty(criteria.RemoteIp))
            {
                query = query.Where(m => m.RemoteIp == criteria.RemoteIp);
            }

            if (criteria.MinSize.HasValue)
            {
                query = query.Where(m => m.Size >= criteria.MinSize.Value);
            }

            if (criteria.MaxSize.HasValue)
            {
                query = query.Where(m => m.Size <= criteria.MaxSize.Value);
            }

            return Task.FromResult(query.OrderByDescending(m => m.ReceivedTime).AsEnumerable());
        }

        public Task<bool> DeleteMessageAsync(string messageId)
        {
            return Task.FromResult(_messages.TryRemove(messageId, out _));
        }

        public Task<bool> ResendMessageAsync(string messageId)
        {
            if (_messages.TryGetValue(messageId, out QueuedMessage? message))
            {
                message.Status = MessageStatus.Queued;
                message.RetryCount++;
                message.LastRetryTime = DateTime.UtcNow;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<QueueStatistics> GetStatisticsAsync()
        {
            List<QueuedMessage> messages = _messages.Values.ToList();
            List<HourlyStats> hourlyStats = GetHourlyStats(messages);

            return Task.FromResult(new QueueStatistics
            {
                TotalMessages = messages.Count,
                QueuedMessages = messages.Count(m => m.Status == MessageStatus.Queued),
                ProcessingMessages = messages.Count(m => m.Status == MessageStatus.Processing),
                FailedMessages = messages.Count(m => m.Status == MessageStatus.Failed),
                DeferredMessages = messages.Count(m => m.Status == MessageStatus.Deferred),
                TotalSize = messages.Sum(m => m.Size),
                AverageSize = messages.Any() ? messages.Average(m => m.Size) : 0,
                AverageRetryCount = messages.Any() ? messages.Average(m => m.RetryCount) : 0,
                MessagesByStatus = messages.GroupBy(m => m.Status)
                    .ToDictionary(g => g.Key, g => g.Count()),
                MessagesByPriority = messages.GroupBy(m => m.Priority)
                    .ToDictionary(g => g.Key, g => g.Count()),
                HourlyStats = hourlyStats
            });
        }

        public Task<int> ClearQueueAsync()
        {
            List<string> queuedMessages = _messages.Values
                .Where(m => m.Status == MessageStatus.Queued)
                .Select(m => m.Id)
                .ToList();

            foreach (string id in queuedMessages)
            {
                _messages.TryRemove(id, out _);
            }

            return Task.FromResult(queuedMessages.Count);
        }

        public Task<string> GetMessageContentAsync(string messageId)
        {
            if (_messages.TryGetValue(messageId, out QueuedMessage? message))
            {
                // In production, this would retrieve the actual message content
                return Task.FromResult($"Content of message {messageId}");
            }

            return Task.FromResult(string.Empty);
        }

        private void SubscribeToEvents()
        {
            _smtpServer.MessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(object? sender, MessageEventArgs e)
        {
            IPEndPoint? remoteEndPoint = e.Session.RemoteEndPoint as IPEndPoint;

            QueuedMessage queuedMessage = new()
            {
                Id = Guid.NewGuid().ToString(),
                MessageId = e.Message.Id,
                From = e.Message.From?.ToString() ?? "unknown",
                To = e.Message.Recipients.Select(r => r.ToString()).ToList(),
                Subject = e.Message.Subject,
                Size = e.Message.Size,
                ReceivedTime = DateTime.UtcNow,
                Status = MessageStatus.Queued,
                Priority = DeterminePriority(e.Message),
                SessionId = e.Session.Id,
                RemoteIp = remoteEndPoint?.Address?.ToString(),
                HasAttachments = false,
                AttachmentNames = []
            };

            // Copy headers
            foreach (KeyValuePair<string, string> header in e.Message.Headers)
            {
                queuedMessage.Headers[header.Key] = header.Value;
            }

            _messages[queuedMessage.Id] = queuedMessage;

            // Clean up old messages (keep last 10000)
            CleanupOldMessages();
        }

        private MessagePriority DeterminePriority(ISmtpMessage message)
        {
            // Check priority headers
            if (message.Headers.TryGetValue("X-Priority", out string? priority))
            {
                if (priority.Contains("1") || priority.Contains("High", StringComparison.OrdinalIgnoreCase))
                {
                    return MessagePriority.High;
                }

                if (priority.Contains("5") || priority.Contains("Low", StringComparison.OrdinalIgnoreCase))
                {
                    return MessagePriority.Low;
                }
            }

            if (message.Headers.TryGetValue("Importance", out string? importance))
            {
                if (importance.Contains("High", StringComparison.OrdinalIgnoreCase))
                {
                    return MessagePriority.High;
                }

                if (importance.Contains("Low", StringComparison.OrdinalIgnoreCase))
                {
                    return MessagePriority.Low;
                }
            }

            return MessagePriority.Normal;
        }

        private void CleanupOldMessages()
        {
            if (_messages.Count > 10000)
            {
                List<string> oldestMessages = _messages.Values
                    .OrderBy(m => m.ReceivedTime)
                    .Take(_messages.Count - 10000)
                    .Select(m => m.Id)
                    .ToList();

                foreach (string id in oldestMessages)
                {
                    _messages.TryRemove(id, out _);
                }
            }
        }

        private List<HourlyStats> GetHourlyStats(List<QueuedMessage> messages)
        {
            List<HourlyStats> stats = [];
            DateTime now = DateTime.UtcNow;

            for (int i = 23; i >= 0; i--)
            {
                DateTime hour = now.AddHours(-i);
                DateTime hourStart = new(hour.Year, hour.Month, hour.Day, hour.Hour, 0, 0);
                DateTime hourEnd = hourStart.AddHours(1);

                List<QueuedMessage> hourMessages = messages.Where(m =>
                    m.ReceivedTime >= hourStart && m.ReceivedTime < hourEnd).ToList();

                stats.Add(new HourlyStats
                {
                    Hour = hourStart,
                    Received = hourMessages.Count,
                    Sent = hourMessages.Count(m => m.Status == MessageStatus.Sent),
                    Failed = hourMessages.Count(m => m.Status == MessageStatus.Failed),
                    Deferred = hourMessages.Count(m => m.Status == MessageStatus.Deferred)
                });
            }

            return stats;
        }
    }
}