using DnsClient;
using DnsClient.Protocol;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Zetian.AntiSpam.Abstractions;
using Zetian.AntiSpam.Models;

namespace Zetian.AntiSpam.Checkers
{
    /// <summary>
    /// SPF (Sender Policy Framework) email authentication checker
    /// </summary>
    public class SpfChecker(SpfConfiguration? configuration = null, ILookupClient? dnsClient = null, ILogger<SpfChecker>? logger = null) : ISpamChecker
    {
        private readonly ILookupClient _dnsClient = dnsClient ?? new LookupClient();
        private readonly SpfConfiguration _configuration = configuration ?? new SpfConfiguration();

        public string Name => "SPF";
        public double Weight => _configuration.Weight;
        public bool IsEnabled => _configuration.Enabled;

        public async Task<SpamCheckResult> CheckAsync(SpamCheckContext context, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled || string.IsNullOrEmpty(context.FromDomain) || context.ClientIpAddress == null)
            {
                return SpamCheckResult.NotSpam(Name);
            }

            try
            {
                string? spfRecord = await GetSpfRecordAsync(context.FromDomain, cancellationToken);
                if (string.IsNullOrEmpty(spfRecord))
                {
                    logger?.LogDebug("No SPF record found for domain {Domain}", context.FromDomain);
                    return HandleNoSpfRecord(context);
                }

                SpfResult result = await ValidateSpfAsync(spfRecord, context, cancellationToken);
                return ConvertToSpamCheckResult(result, context);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "SPF check failed for domain {Domain}", context.FromDomain);
                return HandleSpfError(context);
            }
        }

        private async Task<string?> GetSpfRecordAsync(string domain, CancellationToken cancellationToken)
        {
            try
            {
                IDnsQueryResponse result = await _dnsClient.QueryAsync(domain, QueryType.TXT, cancellationToken: cancellationToken);

                List<string> spfRecords = result.Answers
                    .OfType<TxtRecord>()
                    .SelectMany(r => r.Text)
                    .Where(t => t.StartsWith("v=spf1", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (spfRecords.Count > 1)
                {
                    logger?.LogWarning("Multiple SPF records found for domain {Domain}", domain);
                }

                return spfRecords.FirstOrDefault();
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "Failed to query SPF record for domain {Domain}", domain);
                return null;
            }
        }

        private async Task<SpfResult> ValidateSpfAsync(string spfRecord, SpamCheckContext context, CancellationToken cancellationToken)
        {
            SpfMechanism[] mechanisms = ParseSpfRecord(spfRecord);
            IPAddress clientIp = context.ClientIpAddress!;

            foreach (SpfMechanism mechanism in mechanisms)
            {
                bool match = await CheckMechanismAsync(mechanism, clientIp, context.FromDomain, cancellationToken);

                if (match)
                {
                    return mechanism.Qualifier switch
                    {
                        "+" => SpfResult.Pass,
                        "-" => SpfResult.Fail,
                        "~" => SpfResult.SoftFail,
                        "?" => SpfResult.Neutral,
                        _ => SpfResult.None
                    };
                }
            }

            // Default action if no mechanism matches
            return SpfResult.Neutral;
        }

        private async Task<bool> CheckMechanismAsync(SpfMechanism mechanism, IPAddress clientIp, string domain, CancellationToken cancellationToken)
        {
            switch (mechanism.Type.ToLowerInvariant())
            {
                case "all":
                    return true;

                case "ip4":
                case "ip6":
                    return CheckIpMechanism(mechanism.Value, clientIp);

                case "a":
                    return await CheckARecordAsync(mechanism.Value ?? domain, clientIp, cancellationToken);

                case "mx":
                    return await CheckMxRecordAsync(mechanism.Value ?? domain, clientIp, cancellationToken);

                case "include":
                    if (!string.IsNullOrEmpty(mechanism.Value))
                    {
                        string? includedSpf = await GetSpfRecordAsync(mechanism.Value, cancellationToken);
                        if (!string.IsNullOrEmpty(includedSpf))
                        {
                            SpfResult result = await ValidateSpfAsync(includedSpf, new SpamCheckContext
                            {
                                ClientIpAddress = clientIp,
                                FromDomain = domain
                            }, cancellationToken);
                            return result == SpfResult.Pass;
                        }
                    }
                    return false;

                case "exists":
                    return await CheckExistsAsync(mechanism.Value ?? domain, cancellationToken);

                default:
                    logger?.LogDebug("Unknown SPF mechanism: {Mechanism}", mechanism.Type);
                    return false;
            }
        }

        private bool CheckIpMechanism(string? ipOrRange, IPAddress clientIp)
        {
            if (string.IsNullOrEmpty(ipOrRange))
            {
                return false;
            }

            try
            {
                if (ipOrRange.Contains('/'))
                {
                    // CIDR notation
                    string[] parts = ipOrRange.Split('/');
                    IPAddress network = IPAddress.Parse(parts[0]);
                    int prefixLength = int.Parse(parts[1]);
                    return IsInSubnet(clientIp, network, prefixLength);
                }
                else
                {
                    // Single IP
                    return IPAddress.Parse(ipOrRange).Equals(clientIp);
                }
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "Failed to parse IP mechanism: {Value}", ipOrRange);
                return false;
            }
        }

        private bool IsInSubnet(IPAddress address, IPAddress subnet, int prefixLength)
        {
            byte[] addressBytes = address.GetAddressBytes();
            byte[] subnetBytes = subnet.GetAddressBytes();

            if (addressBytes.Length != subnetBytes.Length)
            {
                return false;
            }

            int bytesToCheck = prefixLength / 8;
            int bitsToCheck = prefixLength % 8;

            for (int i = 0; i < bytesToCheck; i++)
            {
                if (addressBytes[i] != subnetBytes[i])
                {
                    return false;
                }
            }

            if (bitsToCheck > 0 && bytesToCheck < addressBytes.Length)
            {
                byte mask = (byte)(0xFF << (8 - bitsToCheck));
                return (addressBytes[bytesToCheck] & mask) == (subnetBytes[bytesToCheck] & mask);
            }

            return true;
        }

        private async Task<bool> CheckARecordAsync(string domain, IPAddress clientIp, CancellationToken cancellationToken)
        {
            try
            {
                IDnsQueryResponse result = await _dnsClient.QueryAsync(domain, QueryType.A, cancellationToken: cancellationToken);
                List<IPAddress> addresses = result.Answers
                    .OfType<ARecord>()
                    .Select(r => r.Address)
                    .ToList();

                if (clientIp.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    IDnsQueryResponse ipv6Result = await _dnsClient.QueryAsync(domain, QueryType.AAAA, cancellationToken: cancellationToken);
                    addresses.AddRange(ipv6Result.Answers.OfType<AaaaRecord>().Select(r => r.Address));
                }

                return addresses.Any(a => a.Equals(clientIp));
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "Failed to check A record for domain {Domain}", domain);
                return false;
            }
        }

        private async Task<bool> CheckMxRecordAsync(string domain, IPAddress clientIp, CancellationToken cancellationToken)
        {
            try
            {
                IDnsQueryResponse result = await _dnsClient.QueryAsync(domain, QueryType.MX, cancellationToken: cancellationToken);
                IEnumerable<DnsString> mxHosts = result.Answers
                    .OfType<MxRecord>()
                    .OrderBy(r => r.Preference)
                    .Select(r => r.Exchange);

                foreach (DnsString? mxHost in mxHosts)
                {
                    if (await CheckARecordAsync(mxHost.ToString().TrimEnd('.'), clientIp, cancellationToken))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "Failed to check MX record for domain {Domain}", domain);
                return false;
            }
        }

        private async Task<bool> CheckExistsAsync(string domain, CancellationToken cancellationToken)
        {
            try
            {
                IDnsQueryResponse result = await _dnsClient.QueryAsync(domain, QueryType.A, cancellationToken: cancellationToken);
                return result.Answers.Any();
            }
            catch
            {
                return false;
            }
        }

        private SpfMechanism[] ParseSpfRecord(string spfRecord)
        {
            string[] parts = spfRecord.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            List<SpfMechanism> mechanisms = [];

            foreach (string? part in parts.Skip(1)) // Skip "v=spf1"
            {
                if (part.StartsWith("redirect=") || part.StartsWith("exp="))
                {
                    continue; // Handle modifiers separately if needed
                }

                string qualifier = "+";
                string mechanismText = part;

                if ("+-~?".Contains(part[0]))
                {
                    qualifier = part[0].ToString();
                    mechanismText = part[1..];
                }

                int colonIndex = mechanismText.IndexOf(':');
                if (colonIndex > 0)
                {
                    mechanisms.Add(new SpfMechanism
                    {
                        Qualifier = qualifier,
                        Type = mechanismText[..colonIndex],
                        Value = mechanismText[(colonIndex + 1)..]
                    });
                }
                else
                {
                    mechanisms.Add(new SpfMechanism
                    {
                        Qualifier = qualifier,
                        Type = mechanismText
                    });
                }
            }

            return mechanisms.ToArray();
        }

        private SpamCheckResult ConvertToSpamCheckResult(SpfResult spfResult, SpamCheckContext context)
        {
            return spfResult switch
            {
                SpfResult.Pass => new SpamCheckResult
                {
                    IsSpam = false,
                    Score = 0,
                    CheckerName = Name,
                    Action = SpamAction.None,
                    Confidence = 1.0,
                    Details = { ["spf_result"] = "pass" }
                },
                SpfResult.Fail => new SpamCheckResult
                {
                    IsSpam = true,
                    Score = _configuration.FailScore,
                    CheckerName = Name,
                    Action = _configuration.FailAction,
                    Reasons = { $"SPF check failed for {context.FromDomain}" },
                    SmtpResponseCode = 550,
                    SmtpResponseMessage = "SPF check failed",
                    Confidence = 0.9,
                    Details = { ["spf_result"] = "fail" }
                },
                SpfResult.SoftFail => new SpamCheckResult
                {
                    IsSpam = _configuration.SoftFailAsSpam,
                    Score = _configuration.SoftFailScore,
                    CheckerName = Name,
                    Action = _configuration.SoftFailAction,
                    Reasons = { $"SPF soft fail for {context.FromDomain}" },
                    Confidence = 0.6,
                    Details = { ["spf_result"] = "softfail" }
                },
                _ => new SpamCheckResult
                {
                    IsSpam = false,
                    Score = _configuration.NeutralScore,
                    CheckerName = Name,
                    Action = SpamAction.None,
                    Confidence = 0.5,
                    Details = { ["spf_result"] = spfResult.ToString().ToLower() }
                },
            };
        }

        private SpamCheckResult HandleNoSpfRecord(SpamCheckContext context)
        {
            return new SpamCheckResult
            {
                IsSpam = _configuration.NoRecordAsSpam,
                Score = _configuration.NoRecordScore,
                CheckerName = Name,
                Action = _configuration.NoRecordAction,
                Confidence = 0.3,
                Details = { ["spf_result"] = "none" }
            };
        }

        private SpamCheckResult HandleSpfError(SpamCheckContext context)
        {
            return new SpamCheckResult
            {
                IsSpam = false,
                Score = 0,
                CheckerName = Name,
                Action = SpamAction.None,
                Confidence = 0.1,
                Details = { ["spf_result"] = "temperror" }
            };
        }

        private class SpfMechanism
        {
            public string Qualifier { get; set; } = "+";
            public string Type { get; set; } = string.Empty;
            public string? Value { get; set; }
        }

        private enum SpfResult
        {
            None,
            Pass,
            Fail,
            SoftFail,
            Neutral,
            TempError,
            PermError
        }
    }

    /// <summary>
    /// Configuration for SPF checking
    /// </summary>
    public class SpfConfiguration
    {
        public bool Enabled { get; set; } = true;
        public double Weight { get; set; } = 1.0;
        public double FailScore { get; set; } = 80.0;
        public double SoftFailScore { get; set; } = 40.0;
        public double NeutralScore { get; set; } = 10.0;
        public double NoRecordScore { get; set; } = 20.0;
        public bool NoRecordAsSpam { get; set; } = false;
        public bool SoftFailAsSpam { get; set; } = false;
        public SpamAction FailAction { get; set; } = SpamAction.Reject;
        public SpamAction SoftFailAction { get; set; } = SpamAction.Mark;
        public SpamAction NoRecordAction { get; set; } = SpamAction.None;
    }
}