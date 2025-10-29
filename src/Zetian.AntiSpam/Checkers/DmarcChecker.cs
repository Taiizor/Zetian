using DnsClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.AntiSpam.Abstractions;
using Zetian.AntiSpam.Enums;
using Zetian.AntiSpam.Models;

namespace Zetian.AntiSpam.Checkers
{
    /// <summary>
    /// Checks DMARC (Domain-based Message Authentication, Reporting & Conformance) policy
    /// </summary>
    public class DmarcChecker : ISpamChecker
    {
        private readonly ILookupClient _dnsClient;
        private readonly SpfChecker? _spfChecker;
        private readonly DkimChecker? _dkimChecker;
        private readonly double _failScore;
        private readonly double _quarantineScore;
        private readonly double _noneScore;
        private readonly bool _enforcePolicy;
        private readonly bool _checkAlignment;

        public DmarcChecker(
            ILookupClient? dnsClient = null,
            SpfChecker? spfChecker = null,
            DkimChecker? dkimChecker = null,
            double failScore = 70,
            double quarantineScore = 50,
            double noneScore = 0,
            bool enforcePolicy = true,
            bool checkAlignment = true)
        {
            _dnsClient = dnsClient ?? new LookupClient();
            _spfChecker = spfChecker;
            _dkimChecker = dkimChecker;
            _failScore = failScore;
            _quarantineScore = quarantineScore;
            _noneScore = noneScore;
            _enforcePolicy = enforcePolicy;
            _checkAlignment = checkAlignment;
            IsEnabled = true;
        }

        public string Name => "DMARC";

        public bool IsEnabled { get; set; }

        public async Task<SpamCheckResult> CheckAsync(
            ISmtpMessage message,
            ISmtpSession session,
            CancellationToken cancellationToken = default)
        {
            if (!IsEnabled || message.From == null)
            {
                return SpamCheckResult.Clean(0, "DMARC check skipped");
            }

            try
            {
                // Get organizational domain
                string organizationalDomain = GetOrganizationalDomain(message.From.Host);

                // Get DMARC record
                DmarcRecord? dmarcRecord = await GetDmarcRecordAsync(organizationalDomain, cancellationToken);

                if (dmarcRecord == null)
                {
                    // Try exact domain if organizational domain didn't work
                    dmarcRecord = await GetDmarcRecordAsync(message.From.Host, cancellationToken);

                    if (dmarcRecord == null)
                    {
                        return SpamCheckResult.Clean(_noneScore, "No DMARC record found");
                    }
                }

                // Get SPF result if available
                SpamCheckResult? spfResult = _spfChecker != null
                    ? await _spfChecker.CheckAsync(message, session, cancellationToken)
                    : null;

                // Get DKIM result if available  
                SpamCheckResult? dkimResult = _dkimChecker != null
                    ? await _dkimChecker.CheckAsync(message, session, cancellationToken)
                    : null;

                // Check alignment
                bool spfAligned = CheckSpfAlignment(message, spfResult, dmarcRecord.SpfAlignment);
                bool dkimAligned = CheckDkimAlignment(message, dkimResult, dmarcRecord.DkimAlignment);

                // DMARC passes if either SPF or DKIM passes AND is aligned
                bool dmarcPass = (spfAligned && spfResult?.IsSpam == false) ||
                                (dkimAligned && dkimResult?.IsSpam == false);

                // Get effective policy
                DmarcPolicy effectivePolicy = dmarcRecord.GetEffectivePolicy(
                    message.From.Host,
                    organizationalDomain);

                // Check if policy should be applied based on percentage
                bool shouldApply = dmarcRecord.ShouldApplyPolicy();

                if (!dmarcPass)
                {
                    string reason = BuildFailureReason(spfAligned, dkimAligned, spfResult, dkimResult);

                    if (!_enforcePolicy || !shouldApply)
                    {
                        // Report only, don't enforce
                        return SpamCheckResult.Clean(
                            _noneScore,
                            $"DMARC fail (not enforced): {reason}");
                    }

                    return effectivePolicy switch
                    {
                        DmarcPolicy.Reject => SpamCheckResult.Spam(
                                                        _failScore,
                                                        $"DMARC reject policy: {reason}",
                                                        $"Domain: {organizationalDomain}, Policy: {effectivePolicy}"),
                        DmarcPolicy.Quarantine => SpamCheckResult.Spam(
                                                        _quarantineScore,
                                                        $"DMARC quarantine policy: {reason}",
                                                        $"Domain: {organizationalDomain}, Policy: {effectivePolicy}"),
                        _ => SpamCheckResult.Clean(
                                                        _noneScore,
                                                        $"DMARC monitoring mode: {reason}"),
                    };
                }

                return SpamCheckResult.Clean(
                    0,
                    $"DMARC pass for {organizationalDomain}",
                    $"SPF: {(spfAligned ? "aligned" : "not aligned")}, DKIM: {(dkimAligned ? "aligned" : "not aligned")}");
            }
            catch (Exception ex)
            {
                return SpamCheckResult.Clean(0, $"DMARC check error: {ex.Message}");
            }
        }

        private async Task<DmarcRecord?> GetDmarcRecordAsync(string domain, CancellationToken cancellationToken)
        {
            try
            {
                // Query _dmarc.domain
                string query = $"_dmarc.{domain}";
                IDnsQueryResponse result = await _dnsClient.QueryAsync(query, QueryType.TXT, cancellationToken: cancellationToken);

                // Find DMARC record (starts with v=DMARC1)
                string? dmarcTxt = result.Answers
                    .OfType<DnsClient.Protocol.TxtRecord>()
                    .SelectMany(r => r.Text)
                    .FirstOrDefault(t => t.StartsWith("v=DMARC1", StringComparison.OrdinalIgnoreCase));

                if (dmarcTxt == null)
                {
                    return null;
                }

                return DmarcRecord.Parse(dmarcTxt);
            }
            catch
            {
                return null;
            }
        }

        private string GetOrganizationalDomain(string domain)
        {
            // Simplified organizational domain detection
            // In production, use Public Suffix List
            string[] parts = domain.Split('.');

            if (parts.Length <= 2)
            {
                return domain;
            }

            // Check for common TLDs with second-level domains (co.uk, com.br, etc.)
            string[] commonSlds = new[] { "co", "com", "net", "org", "gov", "edu", "ac" };

            if (parts.Length >= 3 && commonSlds.Contains(parts[^2]))
            {
                // domain.co.uk -> return last 3 parts
                return string.Join(".", parts.TakeLast(3));
            }

            // Regular domain - return last 2 parts
            return string.Join(".", parts.TakeLast(2));
        }

        private bool CheckSpfAlignment(
            ISmtpMessage message,
            SpamCheckResult? spfResult,
            DmarcAlignment alignment)
        {
            if (!_checkAlignment || spfResult == null || message.From == null)
            {
                return false;
            }

            // For SPF alignment, check if MAIL FROM domain matches From header domain
            // This is simplified - actual implementation would check SPF authenticated domain

            if (alignment == DmarcAlignment.Strict)
            {
                // Exact domain match required
                return message.From.Host.Equals(message.From.Host, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                // Relaxed - organizational domain match
                string fromOrgDomain = GetOrganizationalDomain(message.From.Host);
                return fromOrgDomain.Equals(fromOrgDomain, StringComparison.OrdinalIgnoreCase);
            }
        }

        private bool CheckDkimAlignment(
            ISmtpMessage message,
            SpamCheckResult? dkimResult,
            DmarcAlignment alignment)
        {
            if (!_checkAlignment || dkimResult == null || message.From == null)
            {
                return false;
            }

            // For DKIM alignment, check if signing domain matches From header domain
            // This is simplified - actual implementation would extract d= domain from DKIM-Signature

            if (!message.Headers.TryGetValue("DKIM-Signature", out string? dkimHeader))
            {
                return false;
            }

            // Extract d= domain from DKIM signature
            Match match = System.Text.RegularExpressions.Regex.Match(dkimHeader, @"d=([^;]+)");
            if (!match.Success)
            {
                return false;
            }

            string signingDomain = match.Groups[1].Value.Trim();

            if (alignment == DmarcAlignment.Strict)
            {
                // Exact domain match required
                return signingDomain.Equals(message.From.Host, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                // Relaxed - organizational domain match
                string fromOrgDomain = GetOrganizationalDomain(message.From.Host);
                string signingOrgDomain = GetOrganizationalDomain(signingDomain);
                return fromOrgDomain.Equals(signingOrgDomain, StringComparison.OrdinalIgnoreCase);
            }
        }

        private string BuildFailureReason(
            bool spfAligned,
            bool dkimAligned,
            SpamCheckResult? spfResult,
            SpamCheckResult? dkimResult)
        {
            var reasons = new List<string>();

            if (spfResult != null)
            {
                if (spfResult.IsSpam)
                {
                    reasons.Add("SPF fail");
                }
                else if (!spfAligned)
                {
                    reasons.Add("SPF not aligned");
                }
            }
            else
            {
                reasons.Add("SPF not checked");
            }

            if (dkimResult != null)
            {
                if (dkimResult.IsSpam)
                {
                    reasons.Add("DKIM fail");
                }
                else if (!dkimAligned)
                {
                    reasons.Add("DKIM not aligned");
                }
            }
            else
            {
                reasons.Add("DKIM not checked");
            }

            return string.Join(", ", reasons);
        }
    }
}