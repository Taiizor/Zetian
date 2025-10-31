using System.Collections.Concurrent;
using System.Net;
using Zetian.Abstractions;
using Zetian.Models.EventArgs;

namespace Zetian.WebUI.Services
{
    /// <summary>
    /// Implementation of session manager
    /// </summary>
    public class SessionManager : ISessionManager
    {
        private readonly ISmtpServer _smtpServer;
        private readonly ConcurrentDictionary<string, SessionInfo> _activeSessions = new();
        private readonly List<SessionInfo> _sessionHistory = [];
        private readonly object _lockObject = new();

        public SessionManager(ISmtpServer smtpServer)
        {
            _smtpServer = smtpServer;

            // Subscribe to server events
            SubscribeToEvents();
        }

        public Task<IEnumerable<SessionInfo>> GetActiveSessionsAsync()
        {
            return Task.FromResult(_activeSessions.Values.AsEnumerable());
        }

        public Task<SessionInfo?> GetSessionAsync(string sessionId)
        {
            _activeSessions.TryGetValue(sessionId, out SessionInfo? session);
            return Task.FromResult(session);
        }

        public Task<IEnumerable<SessionInfo>> GetSessionHistoryAsync(int count = 100)
        {
            lock (_lockObject)
            {
                return Task.FromResult(_sessionHistory
                    .OrderByDescending(s => s.StartTime)
                    .Take(count)
                    .AsEnumerable());
            }
        }

        public Task<bool> DisconnectSessionAsync(string sessionId)
        {
            if (_activeSessions.TryGetValue(sessionId, out SessionInfo? session))
            {
                session.State = SessionState.Closing;
                session.EndTime = DateTime.UtcNow;

                // Move to history
                lock (_lockObject)
                {
                    _sessionHistory.Add(session);

                    // Keep only last 10000 sessions
                    while (_sessionHistory.Count > 10000)
                    {
                        _sessionHistory.RemoveAt(0);
                    }
                }

                _activeSessions.TryRemove(sessionId, out _);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<SessionStatistics> GetStatisticsAsync()
        {
            List<SessionInfo> activeSessions = _activeSessions.Values.ToList();
            List<SessionInfo> allSessions = activeSessions.Concat(_sessionHistory).ToList();

            return Task.FromResult(new SessionStatistics
            {
                ActiveSessions = _activeSessions.Count,
                TotalSessions = allSessions.Count,
                AuthenticatedSessions = activeSessions.Count(s => s.IsAuthenticated),
                SecureSessions = activeSessions.Count(s => s.IsSecure),
                AverageDuration = allSessions.Any()
                    ? allSessions.Average(s => s.Duration.TotalSeconds)
                    : 0,
                TotalBytesReceived = allSessions.Sum(s => s.BytesReceived),
                TotalBytesSent = allSessions.Sum(s => s.BytesSent),
                SessionsByCountry = GetSessionsByCountry(activeSessions),
                SessionsByClient = GetSessionsByClient(activeSessions)
            });
        }

        private void SubscribeToEvents()
        {
            _smtpServer.SessionCreated += OnSessionCreated;
            _smtpServer.SessionCompleted += OnSessionCompleted;
            _smtpServer.MessageReceived += OnMessageReceived;
            _smtpServer.AuthenticationSucceeded += OnAuthenticationSucceeded;
            _smtpServer.CommandExecuted += OnCommandExecuted;
            _smtpServer.DataTransferCompleted += OnDataTransferCompleted;
            _smtpServer.TlsNegotiationCompleted += OnTlsNegotiationCompleted;
        }

        private void OnSessionCreated(object? sender, SessionEventArgs e)
        {
            IPEndPoint? remoteEndPoint = e.Session.RemoteEndPoint as IPEndPoint;
            IPEndPoint? localEndPoint = e.Session.LocalEndPoint as IPEndPoint;

            SessionInfo sessionInfo = new()
            {
                Id = e.Session.Id,
                RemoteAddress = remoteEndPoint?.Address?.ToString() ?? "unknown",
                RemotePort = remoteEndPoint?.Port ?? 0,
                LocalAddress = localEndPoint?.Address?.ToString() ?? "unknown",
                LocalPort = localEndPoint?.Port ?? 0,
                StartTime = DateTime.UtcNow,
                State = SessionState.Connected
            };

            _activeSessions[e.Session.Id] = sessionInfo;
        }

        private void OnSessionCompleted(object? sender, SessionEventArgs e)
        {
            if (_activeSessions.TryRemove(e.Session.Id, out SessionInfo? session))
            {
                session.EndTime = DateTime.UtcNow;
                session.State = SessionState.Closed;

                lock (_lockObject)
                {
                    _sessionHistory.Add(session);

                    // Keep only last 10000 sessions
                    while (_sessionHistory.Count > 10000)
                    {
                        _sessionHistory.RemoveAt(0);
                    }
                }
            }
        }

        private void OnMessageReceived(object? sender, MessageEventArgs e)
        {
            if (_activeSessions.TryGetValue(e.Session.Id, out SessionInfo? session))
            {
                session.MessagesReceived++;
                session.BytesReceived += e.Message.Size;
                session.LastActivity = DateTime.UtcNow;
            }
        }

        private void OnAuthenticationSucceeded(object? sender, AuthenticationEventArgs e)
        {
            if (_activeSessions.TryGetValue(e.Session.Id, out SessionInfo? session))
            {
                session.IsAuthenticated = true;
                session.AuthenticatedUser = e.AuthenticatedIdentity;
                session.State = SessionState.Authenticated;
            }
        }

        private void OnCommandExecuted(object? sender, CommandEventArgs e)
        {
            if (_activeSessions.TryGetValue(e.Session.Id, out SessionInfo? session))
            {
                session.CommandsExecuted++;
                session.LastCommand = e.Command.Verb;
                session.LastActivity = DateTime.UtcNow;
            }
        }

        private void OnDataTransferCompleted(object? sender, DataTransferEventArgs e)
        {
            if (_activeSessions.TryGetValue(e.Session.Id, out SessionInfo? session))
            {
                session.BytesReceived += e.BytesTransferred;
                session.LastActivity = DateTime.UtcNow;
            }
        }

        private void OnTlsNegotiationCompleted(object? sender, TlsEventArgs e)
        {
            if (_activeSessions.TryGetValue(e.Session.Id, out SessionInfo? session))
            {
                session.IsSecure = true;
                session.TlsVersion = e.ProtocolVersion;
                session.CipherSuite = e.CipherSuite;
            }
        }

        private Dictionary<string, int> GetSessionsByCountry(List<SessionInfo> sessions)
        {
            // In production, this would use GeoIP lookup
            return new Dictionary<string, int>
            {
                ["US"] = sessions.Count / 3,
                ["UK"] = sessions.Count / 4,
                ["DE"] = sessions.Count / 5,
                ["Other"] = sessions.Count - ((sessions.Count / 3) + (sessions.Count / 4) + (sessions.Count / 5))
            };
        }

        private Dictionary<string, int> GetSessionsByClient(List<SessionInfo> sessions)
        {
            // Group by remote address
            return sessions
                .GroupBy(s => s.RemoteAddress)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}