using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.AntiSpam.Abstractions;
using Zetian.AntiSpam.Models;

namespace Zetian.AntiSpam.Checkers
{
    /// <summary>
    /// Implements greylisting - temporarily rejects messages from unknown senders
    /// </summary>
    public class GreylistingChecker : ISpamChecker
    {
        private readonly ConcurrentDictionary<string, GreylistEntry> _greylist;
        private readonly TimeSpan _initialDelay;
        private readonly TimeSpan _whitelistDuration;
        private readonly TimeSpan _maxRetryTime;
        private readonly bool _autoWhitelist;
        private readonly Timer _cleanupTimer;

        public GreylistingChecker(
            TimeSpan? initialDelay = null,
            TimeSpan? whitelistDuration = null,
            TimeSpan? maxRetryTime = null,
            bool autoWhitelist = true)
        {
            _greylist = new ConcurrentDictionary<string, GreylistEntry>();
            _initialDelay = initialDelay ?? TimeSpan.FromMinutes(5);
            _whitelistDuration = whitelistDuration ?? TimeSpan.FromDays(30);
            _maxRetryTime = maxRetryTime ?? TimeSpan.FromHours(4);
            _autoWhitelist = autoWhitelist;
            IsEnabled = true;

            // Setup cleanup timer to run every hour
            _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        }

        public string Name => "Greylisting";
        
        public bool IsEnabled { get; set; }

        public async Task<SpamCheckResult> CheckAsync(
            ISmtpMessage message,
            ISmtpSession session,
            CancellationToken cancellationToken = default)
        {
            if (!IsEnabled || message.From == null)
            {
                return SpamCheckResult.Clean(0, "Greylisting disabled or no sender");
            }

            // Create triplet key
            string triplet = CreateTriplet(session, message);
            
            var now = DateTime.UtcNow;

            // Check if already whitelisted
            if (_greylist.TryGetValue(triplet, out GreylistEntry? entry))
            {
                if (entry.IsWhitelisted)
                {
                    // Update last seen time
                    entry.LastSeen = now;
                    return SpamCheckResult.Clean(0, "Sender whitelisted");
                }

                // Check if initial delay has passed
                var timeSinceFirst = now - entry.FirstSeen;
                
                if (timeSinceFirst < _initialDelay)
                {
                    // Still in delay period
                    return SpamCheckResult.Spam(
                        100,
                        "Greylisting delay",
                        $"Please retry after {(_initialDelay - timeSinceFirst).TotalMinutes:F1} minutes");
                }

                if (timeSinceFirst > _maxRetryTime)
                {
                    // Retry came too late, reset
                    entry.FirstSeen = now;
                    entry.LastSeen = now;
                    entry.AttemptCount = 1;
                    
                    return SpamCheckResult.Spam(
                        100,
                        "Greylisting retry too late",
                        "Initial retry period expired, please try again");
                }

                // Valid retry - whitelist if configured
                if (_autoWhitelist)
                {
                    entry.IsWhitelisted = true;
                    entry.WhitelistedAt = now;
                }
                
                entry.LastSeen = now;
                entry.AttemptCount++;
                
                return SpamCheckResult.Clean(0, "Greylisting passed");
            }

            // New triplet - add to greylist
            _greylist.TryAdd(triplet, new GreylistEntry
            {
                Triplet = triplet,
                FirstSeen = now,
                LastSeen = now,
                AttemptCount = 1
            });

            return SpamCheckResult.Spam(
                100,
                "Greylisting active",
                $"Unknown sender, please retry after {_initialDelay.TotalMinutes} minutes");
        }

        /// <summary>
        /// Manually whitelist a sender
        /// </summary>
        public void Whitelist(string senderDomain)
        {
            foreach (var kvp in _greylist)
            {
                if (kvp.Key.Contains(senderDomain, StringComparison.OrdinalIgnoreCase))
                {
                    kvp.Value.IsWhitelisted = true;
                    kvp.Value.WhitelistedAt = DateTime.UtcNow;
                }
            }
        }

        /// <summary>
        /// Clear all greylist entries
        /// </summary>
        public void Clear()
        {
            _greylist.Clear();
        }

        /// <summary>
        /// Get statistics about the greylist
        /// </summary>
        public GreylistStatistics GetStatistics()
        {
            var stats = new GreylistStatistics
            {
                TotalEntries = _greylist.Count
            };

            foreach (var entry in _greylist.Values)
            {
                if (entry.IsWhitelisted)
                {
                    stats.WhitelistedEntries++;
                }
                else
                {
                    stats.GreylistedEntries++;
                }
            }

            return stats;
        }

        private string CreateTriplet(ISmtpSession session, ISmtpMessage message)
        {
            // Create triplet from: client IP, sender, first recipient
            string clientIp = "unknown";
            if (session.RemoteEndPoint is IPEndPoint ipEndPoint)
            {
                // Use /24 for IPv4 or /64 for IPv6 to handle dynamic IPs
                clientIp = GetIpSubnet(ipEndPoint.Address);
            }

            string sender = message.From?.Address ?? "null";
            string recipient = message.Recipients.Count > 0 ? message.Recipients[0].Address : "none";

            return $"{clientIp}|{sender.ToLowerInvariant()}|{recipient.ToLowerInvariant()}";
        }

        private string GetIpSubnet(IPAddress ip)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                // IPv4: use /24 subnet
                byte[] bytes = ip.GetAddressBytes();
                return $"{bytes[0]}.{bytes[1]}.{bytes[2]}.0/24";
            }
            else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                // IPv6: use /64 subnet
                byte[] bytes = ip.GetAddressBytes();
                for (int i = 8; i < 16; i++)
                {
                    bytes[i] = 0;
                }
                return $"{new IPAddress(bytes)}/64";
            }
            
            return ip.ToString();
        }

        private void CleanupExpiredEntries(object? state)
        {
            var now = DateTime.UtcNow;
            var keysToRemove = new List<string>();

            foreach (var kvp in _greylist)
            {
                var entry = kvp.Value;
                
                if (entry.IsWhitelisted)
                {
                    // Remove whitelisted entries after whitelist duration
                    if (now - entry.LastSeen > _whitelistDuration)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
                else
                {
                    // Remove greylist entries not seen for max retry time
                    if (now - entry.LastSeen > _maxRetryTime)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
            }

            foreach (string key in keysToRemove)
            {
                _greylist.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Dispose of resources
        /// </summary>
        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            GC.SuppressFinalize(this);
        }

        private class GreylistEntry
        {
            public string Triplet { get; set; } = string.Empty;
            public DateTime FirstSeen { get; set; }
            public DateTime LastSeen { get; set; }
            public int AttemptCount { get; set; }
            public bool IsWhitelisted { get; set; }
            public DateTime? WhitelistedAt { get; set; }
        }
    }

    /// <summary>
    /// Statistics about greylisting
    /// </summary>
    public class GreylistStatistics
    {
        public int TotalEntries { get; set; }
        public int GreylistedEntries { get; set; }
        public int WhitelistedEntries { get; set; }
    }
}