using DnsClient;
using DnsClient.Protocol;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.AntiSpam.Abstractions;
using Zetian.AntiSpam.Enums;
using Zetian.AntiSpam.Models;

namespace Zetian.AntiSpam.Checkers
{
    /// <summary>
    /// Checks SPF (Sender Policy Framework) records
    /// </summary>
    public class SpfChecker : ISpamChecker
    {
        private readonly ILookupClient _dnsClient;
        private readonly double _failScore;
        private readonly double _softFailScore;
        private readonly double _neutralScore;
        private readonly double _noneScore;

        public SpfChecker(
            ILookupClient? dnsClient = null,
            double failScore = 50,
            double softFailScore = 30,
            double neutralScore = 10,
            double noneScore = 5)
        {
            _dnsClient = dnsClient ?? new LookupClient();
            _failScore = failScore;
            _softFailScore = softFailScore;
            _neutralScore = neutralScore;
            _noneScore = noneScore;
            IsEnabled = true;
        }

        public string Name => "SPF";

        public bool IsEnabled { get; set; }

        public async Task<SpamCheckResult> CheckAsync(
            ISmtpMessage message,
            ISmtpSession session,
            CancellationToken cancellationToken = default)
        {
            if (!IsEnabled || message.From == null)
            {
                return SpamCheckResult.Clean(0, "SPF check skipped");
            }

            try
            {
                string domain = message.From.Host;
                IPAddress? clientIp = GetClientIp(session);

                if (clientIp == null)
                {
                    return SpamCheckResult.Clean(0, "Cannot determine client IP");
                }

                SpfResult result = await CheckSpfAsync(domain, clientIp, cancellationToken);

                return result switch
                {
                    SpfResult.Pass => SpamCheckResult.Clean(0, $"SPF Pass for {domain}"),
                    SpfResult.Fail => SpamCheckResult.Spam(_failScore, "SPF Fail", $"Domain {domain} does not authorize {clientIp}"),
                    SpfResult.SoftFail => SpamCheckResult.Spam(_softFailScore, "SPF SoftFail", $"Domain {domain} discourages use of {clientIp}"),
                    SpfResult.Neutral => SpamCheckResult.Clean(_neutralScore, $"SPF Neutral for {domain}"),
                    SpfResult.None => SpamCheckResult.Clean(_noneScore, $"No SPF record for {domain}"),
                    _ => SpamCheckResult.Clean(0, $"SPF check error for {domain}")
                };
            }
            catch (Exception ex)
            {
                return SpamCheckResult.Clean(0, $"SPF check failed: {ex.Message}");
            }
        }

        private async Task<SpfResult> CheckSpfAsync(string domain, IPAddress clientIp, CancellationToken cancellationToken)
        {
            try
            {
                // Query for SPF record (TXT record starting with "v=spf1")
                IDnsQueryResponse result = await _dnsClient.QueryAsync(domain, QueryType.TXT, cancellationToken: cancellationToken);

                string? spfRecord = result.Answers
                    .OfType<DnsClient.Protocol.TxtRecord>()
                    .SelectMany(r => r.Text)
                    .FirstOrDefault(t => t.StartsWith("v=spf1", StringComparison.OrdinalIgnoreCase));

                if (spfRecord == null)
                {
                    return SpfResult.None;
                }

                // Parse and evaluate SPF record
                return await EvaluateSpfRecordAsync(spfRecord, domain, clientIp, cancellationToken);
            }
            catch (Exception)
            {
                return SpfResult.TempError;
            }
        }

        private async Task<SpfResult> EvaluateSpfRecordAsync(
            string spfRecord,
            string domain,
            IPAddress clientIp,
            CancellationToken cancellationToken)
        {
            string[] mechanisms = spfRecord.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (string mechanism in mechanisms.Skip(1)) // Skip "v=spf1"
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return SpfResult.TempError;
                }

                // Handle "all" mechanism
                if (mechanism == "-all")
                {
                    return SpfResult.Fail;
                }
                if (mechanism == "~all")
                {
                    return SpfResult.SoftFail;
                }
                if (mechanism == "?all")
                {
                    return SpfResult.Neutral;
                }
                if (mechanism is "+all" or "all")
                {
                    return SpfResult.Pass;
                }

                // Handle IP4 mechanism
                if (mechanism.StartsWith("ip4:", StringComparison.OrdinalIgnoreCase))
                {
                    string ipOrRange = mechanism[4..];
                    if (IsIpInRange(clientIp, ipOrRange))
                    {
                        return SpfResult.Pass;
                    }
                }

                // Handle IP6 mechanism
                if (mechanism.StartsWith("ip6:", StringComparison.OrdinalIgnoreCase))
                {
                    string ipOrRange = mechanism[4..];
                    if (IsIpInRange(clientIp, ipOrRange))
                    {
                        return SpfResult.Pass;
                    }
                }

                // Handle A mechanism
                if (mechanism == "a" || mechanism.StartsWith("a:", StringComparison.OrdinalIgnoreCase))
                {
                    string checkDomain = mechanism == "a" ? domain : mechanism[2..];
                    if (await IsIpMatchesARecordAsync(clientIp, checkDomain, cancellationToken))
                    {
                        return SpfResult.Pass;
                    }
                }

                // Handle MX mechanism
                if (mechanism == "mx" || mechanism.StartsWith("mx:", StringComparison.OrdinalIgnoreCase))
                {
                    string checkDomain = mechanism == "mx" ? domain : mechanism[3..];
                    if (await IsIpMatchesMxRecordAsync(clientIp, checkDomain, cancellationToken))
                    {
                        return SpfResult.Pass;
                    }
                }

                // Handle include mechanism
                if (mechanism.StartsWith("include:", StringComparison.OrdinalIgnoreCase))
                {
                    string includeDomain = mechanism[8..];
                    SpfResult includeResult = await CheckSpfAsync(includeDomain, clientIp, cancellationToken);
                    if (includeResult == SpfResult.Pass)
                    {
                        return SpfResult.Pass;
                    }
                }
            }

            return SpfResult.Neutral;
        }

        private bool IsIpInRange(IPAddress ip, string ipOrRange)
        {
            try
            {
                if (ipOrRange.Contains('/'))
                {
                    // CIDR notation
                    string[] parts = ipOrRange.Split('/');
                    if (IPAddress.TryParse(parts[0], out IPAddress? rangeIp) && int.TryParse(parts[1], out int prefixLength))
                    {
                        return IsInSubnet(ip, rangeIp, prefixLength);
                    }
                }
                else if (IPAddress.TryParse(ipOrRange, out IPAddress? singleIp))
                {
                    return ip.Equals(singleIp);
                }
            }
            catch
            {
                // Invalid IP format
            }

            return false;
        }

        private bool IsInSubnet(IPAddress address, IPAddress subnet, int prefixLength)
        {
            byte[] addressBytes = address.GetAddressBytes();
            byte[] subnetBytes = subnet.GetAddressBytes();

            if (addressBytes.Length != subnetBytes.Length)
            {
                return false;
            }

            int fullBytes = prefixLength / 8;
            int remainingBits = prefixLength % 8;

            for (int i = 0; i < fullBytes; i++)
            {
                if (addressBytes[i] != subnetBytes[i])
                {
                    return false;
                }
            }

            if (remainingBits > 0 && fullBytes < addressBytes.Length)
            {
                byte mask = (byte)(0xFF << (8 - remainingBits));
                if ((addressBytes[fullBytes] & mask) != (subnetBytes[fullBytes] & mask))
                {
                    return false;
                }
            }

            return true;
        }

        private async Task<bool> IsIpMatchesARecordAsync(IPAddress ip, string domain, CancellationToken cancellationToken)
        {
            try
            {
                IDnsQueryResponse result = await _dnsClient.QueryAsync(domain, ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? QueryType.AAAA : QueryType.A, cancellationToken: cancellationToken);
                return result.Answers.Any(a =>
                    (a is DnsClient.Protocol.ARecord aRecord && aRecord.Address.Equals(ip)) ||
                    (a is DnsClient.Protocol.AaaaRecord aaaaRecord && aaaaRecord.Address.Equals(ip)));
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> IsIpMatchesMxRecordAsync(IPAddress ip, string domain, CancellationToken cancellationToken)
        {
            try
            {
                IDnsQueryResponse mxResult = await _dnsClient.QueryAsync(domain, QueryType.MX, cancellationToken: cancellationToken);

                foreach (MxRecord mx in mxResult.Answers.OfType<DnsClient.Protocol.MxRecord>())
                {
                    if (await IsIpMatchesARecordAsync(ip, mx.Exchange, cancellationToken))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Ignore DNS errors
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
    }
}