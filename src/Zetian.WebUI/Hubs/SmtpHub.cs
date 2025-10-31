using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zetian.WebUI.Services;

namespace Zetian.WebUI
{
    /// <summary>
    /// SignalR hub for real-time SMTP server updates
    /// </summary>
    [Authorize]
    public class SmtpHub : Hub
    {
        private readonly IDashboardService _dashboardService;
        private readonly ISessionManager _sessionManager;
        private readonly IMessageQueueService _messageQueueService;
        private readonly ILogService _logService;

        public SmtpHub(
            IDashboardService dashboardService,
            ISessionManager sessionManager,
            IMessageQueueService messageQueueService,
            ILogService logService)
        {
            _dashboardService = dashboardService;
            _sessionManager = sessionManager;
            _messageQueueService = messageQueueService;
            _logService = logService;
        }

        /// <summary>
        /// Called when a client connects
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "dashboard");
            await Clients.Caller.SendAsync("Connected", new { connectionId = Context.ConnectionId });

            // Send initial data
            DashboardOverview overview = await _dashboardService.GetOverviewAsync();
            await Clients.Caller.SendAsync("DashboardUpdate", overview);

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "dashboard");
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Subscribes to dashboard updates
        /// </summary>
        public async Task SubscribeToDashboard()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "dashboard");
            await Clients.Caller.SendAsync("SubscribedToDashboard");
        }

        /// <summary>
        /// Unsubscribes from dashboard updates
        /// </summary>
        public async Task UnsubscribeFromDashboard()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "dashboard");
            await Clients.Caller.SendAsync("UnsubscribedFromDashboard");
        }

        /// <summary>
        /// Subscribes to session updates
        /// </summary>
        public async Task SubscribeToSessions()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "sessions");

            // Send current sessions
            IEnumerable<SessionInfo> sessions = await _sessionManager.GetActiveSessionsAsync();
            await Clients.Caller.SendAsync("SessionsList", sessions);
        }

        /// <summary>
        /// Subscribes to message queue updates
        /// </summary>
        public async Task SubscribeToMessages()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "messages");

            // Send current queue
            IEnumerable<QueuedMessage> messages = await _messageQueueService.GetQueuedMessagesAsync();
            await Clients.Caller.SendAsync("MessagesList", messages);
        }

        /// <summary>
        /// Subscribes to log updates
        /// </summary>
        public async Task SubscribeToLogs(string? filter = null, string? level = null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "logs");

            // Send recent logs
            IEnumerable<LogEntry> logs = await _logService.GetRecentLogsAsync(100, filter, level);
            await Clients.Caller.SendAsync("LogsList", logs);
        }

        /// <summary>
        /// Gets dashboard metrics
        /// </summary>
        public async Task<object> GetMetrics()
        {
            return await _dashboardService.GetMetricsAsync();
        }

        /// <summary>
        /// Gets server overview
        /// </summary>
        public async Task<object> GetOverview()
        {
            return await _dashboardService.GetOverviewAsync();
        }

        /// <summary>
        /// Gets active sessions
        /// </summary>
        public async Task<object> GetSessions()
        {
            return await _sessionManager.GetActiveSessionsAsync();
        }

        /// <summary>
        /// Disconnects a session
        /// </summary>
        public async Task DisconnectSession(string sessionId)
        {
            bool result = await _sessionManager.DisconnectSessionAsync(sessionId);
            await Clients.Group("sessions").SendAsync("SessionDisconnected", sessionId);
            await Clients.Caller.SendAsync("DisconnectResult", result);
        }

        /// <summary>
        /// Gets queued messages
        /// </summary>
        public async Task<object> GetMessages(int page = 1, int pageSize = 20)
        {
            return await _messageQueueService.GetPagedMessagesAsync(page, pageSize);
        }

        /// <summary>
        /// Deletes a message
        /// </summary>
        public async Task DeleteMessage(string messageId)
        {
            bool result = await _messageQueueService.DeleteMessageAsync(messageId);
            await Clients.Group("messages").SendAsync("MessageDeleted", messageId);
            await Clients.Caller.SendAsync("DeleteResult", result);
        }

        /// <summary>
        /// Resends a message
        /// </summary>
        public async Task ResendMessage(string messageId)
        {
            bool result = await _messageQueueService.ResendMessageAsync(messageId);
            await Clients.Group("messages").SendAsync("MessageResent", messageId);
            await Clients.Caller.SendAsync("ResendResult", result);
        }

        /// <summary>
        /// Gets recent activity
        /// </summary>
        public async Task<object> GetRecentActivity(int count = 50)
        {
            return await _dashboardService.GetRecentActivityAsync(count);
        }

        /// <summary>
        /// Gets performance data
        /// </summary>
        public async Task<object> GetPerformanceData(DateTime? from = null, DateTime? to = null)
        {
            from ??= DateTime.UtcNow.AddHours(-1);
            to ??= DateTime.UtcNow;
            return await _dashboardService.GetPerformanceDataAsync(from.Value, to.Value);
        }

        /// <summary>
        /// Requests a server restart
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task RestartServer()
        {
            await Clients.All.SendAsync("ServerRestarting");
            // Actual restart logic would be implemented here
        }

        /// <summary>
        /// Broadcasts a message to all connected clients
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task BroadcastMessage(string message, string type = "info")
        {
            await Clients.All.SendAsync("BroadcastMessage", new { message, type, timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Gets health status
        /// </summary>
        public async Task<object> GetHealthStatus()
        {
            return await _dashboardService.GetHealthStatusAsync();
        }

        /// <summary>
        /// Updates client preferences
        /// </summary>
        public async Task UpdatePreferences(object preferences)
        {
            // Store preferences
            await Clients.Caller.SendAsync("PreferencesUpdated", preferences);
        }
    }
}