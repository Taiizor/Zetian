using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zetian.WebUI.Services
{
    /// <summary>
    /// Service for notifications
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Sends a notification to all connected clients
        /// </summary>
        Task BroadcastAsync(Notification notification);

        /// <summary>
        /// Sends a notification to a specific user
        /// </summary>
        Task SendToUserAsync(string userId, Notification notification);

        /// <summary>
        /// Sends a notification to a group
        /// </summary>
        Task SendToGroupAsync(string group, Notification notification);

        /// <summary>
        /// Gets notification history
        /// </summary>
        Task<IEnumerable<Notification>> GetHistoryAsync(int count = 50);

        /// <summary>
        /// Registers notification rules
        /// </summary>
        Task RegisterRuleAsync(NotificationRule rule);

        /// <summary>
        /// Gets notification rules
        /// </summary>
        Task<IEnumerable<NotificationRule>> GetRulesAsync();

        /// <summary>
        /// Deletes a notification rule
        /// </summary>
        Task<bool> DeleteRuleAsync(string ruleId);
    }

    /// <summary>
    /// Notification
    /// </summary>
    public class Notification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public NotificationType Type { get; set; }
        public NotificationLevel Level { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public Dictionary<string, object> Data { get; set; } = [];
        public bool Persistent { get; set; }
        public int? Duration { get; set; }
    }

    /// <summary>
    /// Notification type
    /// </summary>
    public enum NotificationType
    {
        Info,
        SessionCreated,
        SessionCompleted,
        MessageReceived,
        MessageQueued,
        MessageSent,
        MessageFailed,
        Error,
        Warning,
        AuthenticationSuccess,
        AuthenticationFailure,
        ServerStarted,
        ServerStopped,
        ConfigurationChanged,
        RateLimitExceeded,
        Custom
    }

    /// <summary>
    /// Notification level
    /// </summary>
    public enum NotificationLevel
    {
        Info,
        Success,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Notification rule
    /// </summary>
    public class NotificationRule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public NotificationRuleType Type { get; set; }
        public Dictionary<string, object> Conditions { get; set; } = [];
        public NotificationAction Action { get; set; } = new();
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Notification rule type
    /// </summary>
    public enum NotificationRuleType
    {
        ErrorThreshold,
        MessageCount,
        SessionCount,
        AuthenticationFailure,
        RateLimit,
        Custom
    }

    /// <summary>
    /// Notification action
    /// </summary>
    public class NotificationAction
    {
        public NotificationActionType Type { get; set; }
        public Dictionary<string, string> Parameters { get; set; } = [];
    }

    /// <summary>
    /// Notification action type
    /// </summary>
    public enum NotificationActionType
    {
        ShowNotification,
        SendEmail,
        LogToFile,
        ExecuteWebhook,
        Custom
    }
}