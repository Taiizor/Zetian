using DnsClient;
using DnsClient.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.AntiSpam.Abstractions;
using Zetian.AntiSpam.Models;

namespace Zetian.AntiSpam.Checkers
{
    /// <summary>
    /// Checks IP addresses against RBL/DNSBL (Realtime Blackhole Lists)
    /// </summary>
    public class RblChecker(
        ILookupClient? dnsClient = null,
        IEnumerable<RblProvider>? providers = null,
        double scorePerListing = 25,
        double maxScore = 100,
        TimeSpan? timeout = null) : ISpamChecker
    {
        private readonly ILookupClient _dnsClient = dnsClient ?? new LookupClient();
        private readonly List<RblProvider> _providers = providers?.ToList() ?? GetDefaultProviders();
        private readonly TimeSpan _timeout = timeout ?? TimeSpan.FromSeconds(5);

        public string Name => "RBL";

        public bool IsEnabled { get; set; } = true;

        public async Task<SpamCheckResult> CheckAsync(
            ISmtpMessage message,
            ISmtpSession session,
            CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
            {
                return SpamCheckResult.Clean(0, "RBL check disabled");
            }

            IPAddress? clientIp = GetClientIp(session);
            if (clientIp == null)
            {
                return SpamCheckResult.Clean(0, "Cannot determine client IP");
            }

            // Skip private IPs
            if (IsPrivateIp(clientIp))
            {
                return SpamCheckResult.Clean(0, "Private IP address");
            }

            List<string> listings = [];
            List<Task<RblResult>> tasks = [];

            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_timeout);

            foreach (RblProvider? provider in _providers.Where(p => p.IsEnabled))
            {
                tasks.Add(CheckProviderAsync(clientIp, provider, cts.Token));
            }

            try
            {
                RblResult[] results = await Task.WhenAll(tasks);
                listings.AddRange(results.Where(r => r.IsListed).Select(r => r.Provider));
            }
            catch (OperationCanceledException)
            {
                // Timeout - use partial results
            }

            if (listings.Count == 0)
            {
                return SpamCheckResult.Clean(0, $"IP {clientIp} not found in any RBL");
            }

            double score = Math.Min(listings.Count * scorePerListing, maxScore);
            string reason = $"Listed in {listings.Count} RBL(s)";
            string details = $"IP {clientIp} listed in: {string.Join(", ", listings)}";

            return SpamCheckResult.Spam(score, reason, details);
        }

        /// <summary>
        /// Adds a custom RBL provider
        /// </summary>
        public void AddProvider(RblProvider provider)
        {
            _providers.Add(provider);
        }

        /// <summary>
        /// Removes an RBL provider
        /// </summary>
        public void RemoveProvider(string name)
        {
            _providers.RemoveAll(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the list of configured providers
        /// </summary>
        public IReadOnlyList<RblProvider> GetProviders()
        {
            return _providers.AsReadOnly();
        }

        private async Task<RblResult> CheckProviderAsync(IPAddress ip, RblProvider provider, CancellationToken cancellationToken)
        {
            try
            {
                string query = BuildRblQuery(ip, provider.Zone);

                IDnsQueryResponse result = await _dnsClient.QueryAsync(query, QueryType.A, cancellationToken: cancellationToken);

                if (result.Answers.Count > 0)
                {
                    // Check if the response matches expected patterns
                    List<ARecord> aRecords = result.Answers.OfType<ARecord>().ToList();

                    if (provider.ExpectedResponses != null && provider.ExpectedResponses.Count > 0)
                    {
                        // Check if response matches any expected patterns
                        foreach (ARecord record in aRecords)
                        {
                            string responseIp = record.Address.ToString();
                            if (provider.ExpectedResponses.Any(responseIp.StartsWith))
                            {
                                return new RblResult { IsListed = true, Provider = provider.Name };
                            }
                        }
                        return new RblResult { IsListed = false, Provider = provider.Name };
                    }

                    // Any A record response means listed
                    return new RblResult { IsListed = true, Provider = provider.Name };
                }

                return new RblResult { IsListed = false, Provider = provider.Name };
            }
            catch (Exception)
            {
                // DNS query failed - assume not listed
                return new RblResult { IsListed = false, Provider = provider.Name };
            }
        }

        private static string BuildRblQuery(IPAddress ip, string zone)
        {
            byte[] bytes = ip.GetAddressBytes();

            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                // IPv4: reverse octets
                Array.Reverse(bytes);
                return $"{string.Join(".", bytes)}.{zone}";
            }
            else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // IPv6: reverse nibbles
                string hex = string.Concat(bytes.Select(b => b.ToString("x2")));
                char[] chars = hex.ToCharArray();
                Array.Reverse(chars);
                return $"{string.Join(".", chars)}.{zone}";
            }

            throw new ArgumentException($"Unsupported IP address family: {ip.AddressFamily}");
        }

        private static bool IsPrivateIp(IPAddress ip)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] bytes = ip.GetAddressBytes();

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
            else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // Check for IPv6 private addresses
                if (IPAddress.IsLoopback(ip))
                {
                    return true;
                }

                byte[] bytes = ip.GetAddressBytes();

                // fc00::/7 (Unique Local Addresses)
                if ((bytes[0] & 0xfe) == 0xfc)
                {
                    return true;
                }

                // fe80::/10 (Link-Local)
                if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80)
                {
                    return true;
                }
            }

            return false;
        }

        private IPAddress? GetClientIp(ISmtpSession session)
        {
            if (session.RemoteEndPoint is IPEndPoint ipEndPoint)
            {
                return ipEndPoint.Address;
            }
            return null;
        }

        private static List<RblProvider> GetDefaultProviders()
        {
            return
            [
                new RblProvider
                {
                    Name = "Spamhaus ZEN",
                    Zone = "zen.spamhaus.org",
                    Description = "Spamhaus Block List",
                    IsEnabled = true,
                    ExpectedResponses = ["127.0.0."]
                },
                new RblProvider
                {
                    Name = "SpamCop",
                    Zone = "bl.spamcop.net",
                    Description = "SpamCop Blocking List",
                    IsEnabled = true,
                    ExpectedResponses = ["127.0.0.2"]
                },
                new RblProvider
                {
                    Name = "Barracuda",
                    Zone = "b.barracudacentral.org",
                    Description = "Barracuda Reputation Block List",
                    IsEnabled = true,
                    ExpectedResponses = ["127.0.0.2"]
                },
                new RblProvider
                {
                    Name = "SORBS",
                    Zone = "dnsbl.sorbs.net",
                    Description = "SORBS DNSBL",
                    IsEnabled = false, // Often has false positives
                    ExpectedResponses = ["127.0.0."]
                },
                new RblProvider
                {
                    Name = "UCEPROTECT L1",
                    Zone = "dnsbl-1.uceprotect.net",
                    Description = "UCEPROTECT Level 1",
                    IsEnabled = false, // Can be aggressive
                    ExpectedResponses = ["127.0.0.2"]
                }
            ];
        }

        private class RblResult
        {
            public bool IsListed { get; set; }
            public string Provider { get; set; } = string.Empty;
        }
    }

    /// <summary>
    /// Represents an RBL/DNSBL provider
    /// </summary>
    public class RblProvider
    {
        /// <summary>
        /// Gets or sets the provider name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the DNS zone to query
        /// </summary>
        public string Zone { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the provider description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets whether this provider is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets expected response patterns (e.g., "127.0.0.")
        /// </summary>
        public List<string>? ExpectedResponses { get; set; }
    }
}