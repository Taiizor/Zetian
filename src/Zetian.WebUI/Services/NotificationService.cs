using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Zetian.Abstractions;

namespace Zetian.WebUI.Services
{
    /// <summary>
    /// Implementation of notification service
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ISmtpServer _smtpServer;
        private readonly IHubContext<SmtpHub>? _hubContext;
        private readonly ConcurrentQueue<Notification> _notificationHistory = new();
        private readonly List<NotificationRule> _rules = [];
        private readonly object _lockObject = new();

        public NotificationService(ISmtpServer smtpServer, IHubContext<SmtpHub>? hubContext = null)
        {
            _smtpServer = smtpServer;
            _hubContext = hubContext;

            // Subscribe to server events
            SubscribeToEvents();
        }

        public async Task BroadcastAsync(Notification notification)
        {
            // Add to history
            AddToHistory(notification);

            // Broadcast via SignalR if available
            if (_hubContext != null)
            {
                await _hubContext.Clients.All.SendAsync("Notification", notification);
            }

            // Apply notification rules
            await ApplyRulesAsync(notification);
        }

        public async Task SendToUserAsync(string userId, Notification notification)
        {
            // Add to history
            AddToHistory(notification);

            // Send via SignalR if available
            if (_hubContext != null)
            {
                await _hubContext.Clients.User(userId).SendAsync("Notification", notification);
            }
        }

        public async Task SendToGroupAsync(string group, Notification notification)
        {
            // Add to history
            AddToHistory(notification);

            // Send via SignalR if available
            if (_hubContext != null)
            {
                await _hubContext.Clients.Group(group).SendAsync("Notification", notification);
            }
        }

        public Task<IEnumerable<Notification>> GetHistoryAsync(int count = 50)
        {
            return Task.FromResult(_notificationHistory
                .OrderByDescending(n => n.Timestamp)
                .Take(count)
                .AsEnumerable());
        }

        public Task RegisterRuleAsync(NotificationRule rule)
        {
            lock (_lockObject)
            {
                _rules.Add(rule);
            }

            return Task.CompletedTask;
        }

        public Task<IEnumerable<NotificationRule>> GetRulesAsync()
        {
            lock (_lockObject)
            {
                return Task.FromResult(_rules.AsEnumerable());
            }
        }

        public Task<bool> DeleteRuleAsync(string ruleId)
        {
            lock (_lockObject)
            {
                NotificationRule? rule = _rules.FirstOrDefault(r => r.Id == ruleId);
                if (rule != null)
                {
                    _rules.Remove(rule);
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        private void SubscribeToEvents()
        {
            _smtpServer.SessionCreated += async (sender, e) =>
            {
                await BroadcastAsync(new Notification
                {
                    Type = NotificationType.SessionCreated,
                    Level = NotificationLevel.Info,
                    Title = "New Session",
                    Message = $"Session created from {e.Session.RemoteEndPoint}",
                    Data = new Dictionary<string, object>
                    {
                        ["sessionId"] = e.Session.Id,
                        ["remoteEndPoint"] = e.Session.RemoteEndPoint?.ToString() ?? "unknown"
                    }
                });
            };

            _smtpServer.SessionCompleted += async (sender, e) =>
            {
                await BroadcastAsync(new Notification
                {
                    Type = NotificationType.SessionCompleted,
                    Level = NotificationLevel.Info,
                    Title = "Session Completed",
                    Message = $"Session {e.Session.Id} completed",
                    Data = new Dictionary<string, object>
                    {
                        ["sessionId"] = e.Session.Id
                    }
                });
            };

            _smtpServer.MessageReceived += async (sender, e) =>
            {
                await BroadcastAsync(new Notification
                {
                    Type = NotificationType.MessageReceived,
                    Level = NotificationLevel.Success,
                    Title = "Message Received",
                    Message = $"New message from {e.Message.From}",
                    Data = new Dictionary<string, object>
                    {
                        ["messageId"] = e.Message.Id,
                        ["from"] = e.Message.From?.ToString() ?? "unknown",
                        ["subject"] = e.Message.Subject ?? "",
                        ["size"] = e.Message.Size
                    },
                    Duration = 5000
                });
            };

            _smtpServer.ErrorOccurred += async (sender, e) =>
            {
                await BroadcastAsync(new Notification
                {
                    Type = NotificationType.Error,
                    Level = NotificationLevel.Error,
                    Title = "Error Occurred",
                    Message = e.Exception.Message,
                    Data = new Dictionary<string, object>
                    {
                        ["exception"] = e.Exception.GetType().Name,
                        ["message"] = e.Exception.Message
                    },
                    Persistent = true
                });
            };

            _smtpServer.AuthenticationSucceeded += async (sender, e) =>
            {
                await BroadcastAsync(new Notification
                {
                    Type = NotificationType.AuthenticationSuccess,
                    Level = NotificationLevel.Success,
                    Title = "Authentication Success",
                    Message = $"User {e.AuthenticatedIdentity} authenticated successfully",
                    Data = new Dictionary<string, object>
                    {
                        ["user"] = e.AuthenticatedIdentity ?? "unknown",
                        ["mechanism"] = e.Mechanism
                    }
                });
            };

            _smtpServer.AuthenticationFailed += async (sender, e) =>
            {
                await BroadcastAsync(new Notification
                {
                    Type = NotificationType.AuthenticationFailure,
                    Level = NotificationLevel.Warning,
                    Title = "Authentication Failed",
                    Message = $"Authentication failed for {e.Username}",
                    Data = new Dictionary<string, object>
                    {
                        ["user"] = e.Username ?? "unknown",
                        ["mechanism"] = e.Mechanism
                    }
                });
            };

            _smtpServer.RateLimitExceeded += async (sender, e) =>
            {
                await BroadcastAsync(new Notification
                {
                    Type = NotificationType.RateLimitExceeded,
                    Level = NotificationLevel.Warning,
                    Title = "Rate Limit Exceeded",
                    Message = $"Rate limit exceeded for {e.IpAddress}",
                    Data = new Dictionary<string, object>
                    {
                        ["ipAddress"] = e.IpAddress,
                        ["limit"] = e.Limit,
                        ["resetAt"] = e.ResetTime
                    },
                    Persistent = true
                });
            };
        }

        private void AddToHistory(Notification notification)
        {
            _notificationHistory.Enqueue(notification);

            // Keep only last 1000 notifications
            while (_notificationHistory.Count > 1000)
            {
                _notificationHistory.TryDequeue(out _);
            }
        }

        private async Task ApplyRulesAsync(Notification notification)
        {
            List<NotificationRule> rulesToApply;
            lock (_lockObject)
            {
                rulesToApply = _rules.Where(r => r.Enabled && ShouldApplyRule(r, notification)).ToList();
            }

            foreach (NotificationRule rule in rulesToApply)
            {
                await ExecuteActionAsync(rule.Action, notification);
            }
        }

        private bool ShouldApplyRule(NotificationRule rule, Notification notification)
        {
            return rule.Type switch
            {
                NotificationRuleType.ErrorThreshold => notification.Type == NotificationType.Error &&
                                           rule.Conditions.TryGetValue("threshold", out object? threshold) &&
                                           GetErrorCount() > Convert.ToInt32(threshold),
                NotificationRuleType.MessageCount => notification.Type == NotificationType.MessageReceived &&
                                           rule.Conditions.TryGetValue("count", out object? count) &&
                                           GetMessageCount() > Convert.ToInt32(count),
                NotificationRuleType.AuthenticationFailure => notification.Type == NotificationType.AuthenticationFailure &&
                                           rule.Conditions.TryGetValue("maxFailures", out object? maxFailures) &&
                                           GetAuthFailureCount() > Convert.ToInt32(maxFailures),
                NotificationRuleType.RateLimit => notification.Type == NotificationType.RateLimitExceeded,
                _ => false,
            };
        }

        private async Task ExecuteActionAsync(NotificationAction action, Notification notification)
        {
            switch (action.Type)
            {
                case NotificationActionType.ShowNotification:
                    // Already shown
                    break;

                case NotificationActionType.SendEmail:
                    // In production, send email
                    Console.WriteLine($"Would send email: {notification.Message}");
                    break;

                case NotificationActionType.LogToFile:
                    // In production, log to file
                    Console.WriteLine($"Would log to file: {notification.Message}");
                    break;

                case NotificationActionType.ExecuteWebhook:
                    // In production, call webhook
                    if (action.Parameters.TryGetValue("url", out string? url))
                    {
                        Console.WriteLine($"Would call webhook: {url}");
                    }
                    break;
            }

            await Task.CompletedTask;
        }

        private int GetErrorCount()
        {
            return _notificationHistory.Count(n => n.Type == NotificationType.Error &&
                                                   n.Timestamp > DateTime.UtcNow.AddMinutes(-5));
        }

        private int GetMessageCount()
        {
            return _notificationHistory.Count(n => n.Type == NotificationType.MessageReceived &&
                                                   n.Timestamp > DateTime.UtcNow.AddMinutes(-1));
        }

        private int GetAuthFailureCount()
        {
            return _notificationHistory.Count(n => n.Type == NotificationType.AuthenticationFailure &&
                                                   n.Timestamp > DateTime.UtcNow.AddMinutes(-10));
        }
    }
}