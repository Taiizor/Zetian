using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zetian.WebUI.Services
{
    /// <summary>
    /// Service for managing message queue
    /// </summary>
    public interface IMessageQueueService
    {
        /// <summary>
        /// Gets all queued messages
        /// </summary>
        Task<IEnumerable<QueuedMessage>> GetQueuedMessagesAsync();

        /// <summary>
        /// Gets paged messages
        /// </summary>
        Task<PagedResult<QueuedMessage>> GetPagedMessagesAsync(int page, int pageSize);

        /// <summary>
        /// Gets a specific message by ID
        /// </summary>
        Task<QueuedMessage?> GetMessageAsync(string messageId);

        /// <summary>
        /// Searches messages
        /// </summary>
        Task<IEnumerable<QueuedMessage>> SearchMessagesAsync(MessageSearchCriteria criteria);

        /// <summary>
        /// Deletes a message
        /// </summary>
        Task<bool> DeleteMessageAsync(string messageId);

        /// <summary>
        /// Resends a message
        /// </summary>
        Task<bool> ResendMessageAsync(string messageId);

        /// <summary>
        /// Gets queue statistics
        /// </summary>
        Task<QueueStatistics> GetStatisticsAsync();

        /// <summary>
        /// Clears the queue
        /// </summary>
        Task<int> ClearQueueAsync();

        /// <summary>
        /// Gets message content
        /// </summary>
        Task<string> GetMessageContentAsync(string messageId);
    }

    /// <summary>
    /// Queued message information
    /// </summary>
    public class QueuedMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? MessageId { get; set; }
        public string From { get; set; } = "";
        public List<string> To { get; set; } = [];
        public string? Subject { get; set; }
        public long Size { get; set; }
        public DateTime ReceivedTime { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public MessageStatus Status { get; set; }
        public MessagePriority Priority { get; set; }
        public int RetryCount { get; set; }
        public DateTime? LastRetryTime { get; set; }
        public string? LastError { get; set; }
        public string? SessionId { get; set; }
        public string? RemoteIp { get; set; }
        public bool HasAttachments { get; set; }
        public List<string> AttachmentNames { get; set; } = [];
        public Dictionary<string, string> Headers { get; set; } = [];
        public Dictionary<string, object> Metadata { get; set; } = [];
    }

    /// <summary>
    /// Message status
    /// </summary>
    public enum MessageStatus
    {
        Queued,
        Processing,
        Sent,
        Failed,
        Deferred,
        Bounced,
        Expired,
        Cancelled
    }

    /// <summary>
    /// Message priority
    /// </summary>
    public enum MessagePriority
    {
        Low,
        Normal,
        High,
        Urgent
    }

    /// <summary>
    /// Message search criteria
    /// </summary>
    public class MessageSearchCriteria
    {
        public string? From { get; set; }
        public string? To { get; set; }
        public string? Subject { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public MessageStatus? Status { get; set; }
        public MessagePriority? Priority { get; set; }
        public string? SessionId { get; set; }
        public string? RemoteIp { get; set; }
        public long? MinSize { get; set; }
        public long? MaxSize { get; set; }
    }

    /// <summary>
    /// Queue statistics
    /// </summary>
    public class QueueStatistics
    {
        public int TotalMessages { get; set; }
        public int QueuedMessages { get; set; }
        public int ProcessingMessages { get; set; }
        public int FailedMessages { get; set; }
        public int DeferredMessages { get; set; }
        public long TotalSize { get; set; }
        public double AverageSize { get; set; }
        public double AverageRetryCount { get; set; }
        public Dictionary<MessageStatus, int> MessagesByStatus { get; set; } = [];
        public Dictionary<MessagePriority, int> MessagesByPriority { get; set; } = [];
        public List<HourlyStats> HourlyStats { get; set; } = [];
    }

    /// <summary>
    /// Hourly statistics
    /// </summary>
    public class HourlyStats
    {
        public DateTime Hour { get; set; }
        public int Received { get; set; }
        public int Sent { get; set; }
        public int Failed { get; set; }
        public int Deferred { get; set; }
    }

    /// <summary>
    /// Paged result
    /// </summary>
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = Array.Empty<T>();
        public int TotalItems { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }
}