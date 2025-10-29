using DnsClient;
using DnsClient.Protocol;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Zetian.AntiSpam.Abstractions;
using Zetian.AntiSpam.Models;

namespace Zetian.AntiSpam.Checkers
{
    /// <summary>
    /// RBL/DNSBL (Realtime Blackhole List) checker for IP reputation
    /// </summary>
    public class RblChecker(RblConfiguration? configuration = null, ILookupClient? dnsClient = null, ILogger<RblChecker>? logger = null) : ISpamChecker
    {
        private readonly ILookupClient _dnsClient = dnsClient ?? new LookupClient();
        private readonly RblConfiguration _configuration = configuration ?? new RblConfiguration();

        public string Name => "RBL";
        public double Weight => _configuration.Weight;
        public bool IsEnabled => _configuration.Enabled;

        public async Task<SpamCheckResult> CheckAsync(SpamCheckContext context, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled || context.ClientIpAddress == null)
            {
                return SpamCheckResult.NotSpam(Name);
            }

            // Skip RBL checks for authenticated users if configured
            if (context.IsAuthenticated && _configuration.SkipForAuthenticatedUsers)
            {
                logger?.LogDebug("Skipping RBL check for authenticated user {User}", context.AuthenticatedUser);
                return SpamCheckResult.NotSpam(Name);
            }

            // Skip RBL checks for private/local IPs
            if (IsPrivateIp(context.ClientIpAddress))
            {
                logger?.LogDebug("Skipping RBL check for private IP {IP}", context.ClientIpAddress);
                return SpamCheckResult.NotSpam(Name);
            }

            List<RblServer> listedServers = [];
            List<Task<RblCheckResult>> tasks = [];

            foreach (RblServer? server in _configuration.Servers.Where(s => s.Enabled))
            {
                tasks.Add(CheckServerAsync(server, context.ClientIpAddress, cancellationToken));
            }

            RblCheckResult[] results = await Task.WhenAll(tasks);

            foreach (RblCheckResult? result in results.Where(r => r.IsListed))
            {
                listedServers.Add(result.Server);
            }

            return BuildSpamCheckResult(listedServers, context);
        }

        private async Task<RblCheckResult> CheckServerAsync(RblServer server, IPAddress ipAddress, CancellationToken cancellationToken)
        {
            try
            {
                string queryHost = BuildRblQuery(ipAddress, server.Host);
                IDnsQueryResponse result = await _dnsClient.QueryAsync(queryHost, QueryType.A, cancellationToken: cancellationToken);

                if (result.Answers.Count > 0)
                {
                    IPAddress? returnCode = result.Answers.OfType<ARecord>().FirstOrDefault()?.Address;
                    bool isListed = IsListedResponse(returnCode, server);

                    if (isListed)
                    {
                        logger?.LogWarning("IP {IP} is listed in RBL {Server}: {ReturnCode}",
                            ipAddress, server.Name, returnCode);
                    }

                    return new RblCheckResult
                    {
                        Server = server,
                        IsListed = isListed,
                        ReturnCode = returnCode?.ToString()
                    };
                }

                return new RblCheckResult { Server = server, IsListed = false };
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "RBL check failed for {IP} on {Server}", ipAddress, server.Name);
                return new RblCheckResult { Server = server, IsListed = false, Error = ex.Message };
            }
        }

        private string BuildRblQuery(IPAddress ipAddress, string rblHost)
        {
            if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                // IPv4: reverse the octets
                byte[] octets = ipAddress.GetAddressBytes();
                Array.Reverse(octets);
                return $"{string.Join(".", octets)}.{rblHost}";
            }
            else if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                // IPv6: reverse the nibbles
                byte[] bytes = ipAddress.GetAddressBytes();
                List<string> nibbles = [];

                foreach (byte b in bytes)
                {
                    nibbles.Add((b & 0x0F).ToString("x"));
                    nibbles.Add((b >> 4).ToString("x"));
                }

                nibbles.Reverse();
                return $"{string.Join(".", nibbles)}.{rblHost}";
            }

            throw new NotSupportedException($"Unsupported IP address family: {ipAddress.AddressFamily}");
        }

        private bool IsListedResponse(IPAddress? returnCode, RblServer server)
        {
            if (returnCode == null)
            {
                return false;
            }

            // Most RBLs return 127.0.0.x codes where x indicates the listing type
            byte[] returnBytes = returnCode.GetAddressBytes();

            // Check if it's in the 127.0.0.x range (standard RBL response)
            if (returnBytes[0] == 127 && returnBytes[1] == 0 && returnBytes[2] == 0)
            {
                // If the server has specific return codes configured, check them
                if (server.ListedReturnCodes?.Any() == true)
                {
                    return server.ListedReturnCodes.Contains(returnBytes[3]);
                }

                // Otherwise, any 127.0.0.x response indicates listing
                return true;
            }

            // Some RBLs might use different ranges
            return server.CustomListingCheck?.Invoke(returnCode) ?? false;
        }

        private bool IsPrivateIp(IPAddress ipAddress)
        {
            if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                byte[] bytes = ipAddress.GetAddressBytes();

                // 10.0.0.0/8
                if (bytes[0] == 10)
                {
                    return true;
                }

                // 172.16.0.0/12
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                {
                    return true;
                }

                // 192.168.0.0/16
                if (bytes[0] == 192 && bytes[1] == 168)
                {
                    return true;
                }

                // 127.0.0.0/8 (loopback)
                if (bytes[0] == 127)
                {
                    return true;
                }
            }
            else if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                // Check for IPv6 private addresses
                return ipAddress.IsIPv6LinkLocal || ipAddress.IsIPv6SiteLocal;
            }

            return false;
        }

        private SpamCheckResult BuildSpamCheckResult(List<RblServer> listedServers, SpamCheckContext context)
        {
            if (!listedServers.Any())
            {
                return new SpamCheckResult
                {
                    IsSpam = false,
                    Score = 0,
                    CheckerName = Name,
                    Action = SpamAction.None,
                    Details = { ["rbl_checked"] = _configuration.Servers.Count(s => s.Enabled) }
                };
            }

            // Calculate score based on number and severity of listings
            double totalScore = 0.0;
            List<string> reasons = [];
            int highSeverityCount = 0;

            foreach (RblServer server in listedServers)
            {
                totalScore += server.Score;
                reasons.Add($"Listed in {server.Name}");

                if (server.Severity == RblSeverity.High)
                {
                    highSeverityCount++;
                }
            }

            // Determine action based on severity and count
            SpamAction action = DetermineAction(listedServers, highSeverityCount);

            // Normalize score to 0-100 range
            double normalizedScore = Math.Min(100, totalScore);

            return new SpamCheckResult
            {
                IsSpam = normalizedScore >= _configuration.SpamThreshold,
                Score = normalizedScore,
                CheckerName = Name,
                Action = action,
                Reasons = reasons,
                Confidence = Math.Min(1.0, listedServers.Count / 3.0), // More listings = higher confidence
                SmtpResponseCode = action == SpamAction.Reject ? 550 : null,
                SmtpResponseMessage = action == SpamAction.Reject
                    ? $"Your IP {context.ClientIpAddress} is listed in {listedServers.Count} blacklist(s)"
                    : null,
                Details =
                {
                    ["rbl_listings"] = listedServers.Select(s => s.Name).ToList(),
                    ["rbl_count"] = listedServers.Count,
                    ["ip_address"] = context.ClientIpAddress?.ToString() ?? ""
                }
            };
        }

        private SpamAction DetermineAction(List<RblServer> listedServers, int highSeverityCount)
        {
            // If listed in any high-severity RBL, reject
            if (highSeverityCount > 0 || listedServers.Count >= _configuration.RejectThreshold)
            {
                return SpamAction.Reject;
            }

            // If listed in multiple medium-severity RBLs, quarantine
            if (listedServers.Count >= _configuration.QuarantineThreshold)
            {
                return SpamAction.Quarantine;
            }

            // If listed in a few low-severity RBLs, just mark
            if (listedServers.Count > 0)
            {
                return SpamAction.Mark;
            }

            return SpamAction.None;
        }

        private class RblCheckResult
        {
            public RblServer Server { get; set; } = new();
            public bool IsListed { get; set; }
            public string? ReturnCode { get; set; }
            public string? Error { get; set; }
        }
    }

    /// <summary>
    /// Configuration for RBL checking
    /// </summary>
    public class RblConfiguration
    {
        public bool Enabled { get; set; } = true;
        public double Weight { get; set; } = 1.5;
        public double SpamThreshold { get; set; } = 50.0;
        public int RejectThreshold { get; set; } = 2;
        public int QuarantineThreshold { get; set; } = 1;
        public bool SkipForAuthenticatedUsers { get; set; } = true;
        public List<RblServer> Servers { get; set; } =
        [
            // Popular and reliable RBLs
            new RblServer
            {
                Name = "Spamhaus ZEN",
                Host = "zen.spamhaus.org",
                Score = 50,
                Severity = RblSeverity.High,
                Enabled = true
            },
            new RblServer
            {
                Name = "Barracuda",
                Host = "b.barracudacentral.org",
                Score = 40,
                Severity = RblSeverity.High,
                Enabled = true
            },
            new RblServer
            {
                Name = "SpamCop",
                Host = "bl.spamcop.net",
                Score = 35,
                Severity = RblSeverity.Medium,
                Enabled = true
            },
            new RblServer
            {
                Name = "SURBL",
                Host = "multi.surbl.org",
                Score = 30,
                Severity = RblSeverity.Medium,
                Enabled = false // Disabled by default, SURBL is for URLs not IPs
            },
            new RblServer
            {
                Name = "UCEPROTECT L1",
                Host = "dnsbl-1.uceprotect.net",
                Score = 25,
                Severity = RblSeverity.Low,
                Enabled = false
            },
            new RblServer
            {
                Name = "Spamhaus XBL",
                Host = "xbl.spamhaus.org",
                Score = 45,
                Severity = RblSeverity.High,
                Enabled = false // Already covered by ZEN
            },
            new RblServer
            {
                Name = "Spamhaus PBL",
                Host = "pbl.spamhaus.org",
                Score = 20,
                Severity = RblSeverity.Low,
                Enabled = false // Policy block list, not spam
            }
        ];
    }

    /// <summary>
    /// Represents an RBL server configuration
    /// </summary>
    public class RblServer
    {
        public string Name { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public double Score { get; set; } = 30.0;
        public RblSeverity Severity { get; set; } = RblSeverity.Medium;
        public bool Enabled { get; set; } = true;
        public List<int>? ListedReturnCodes { get; set; }
        public Func<IPAddress, bool>? CustomListingCheck { get; set; }
    }

    /// <summary>
    /// RBL severity levels
    /// </summary>
    public enum RblSeverity
    {
        Low,
        Medium,
        High
    }
}