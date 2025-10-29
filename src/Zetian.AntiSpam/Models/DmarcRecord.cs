using System;
using System.Collections.Generic;
using System.Linq;
using Zetian.AntiSpam.Enums;

namespace Zetian.AntiSpam.Models
{
    /// <summary>
    /// Represents a parsed DMARC DNS record
    /// </summary>
    public class DmarcRecord
    {
        /// <summary>
        /// DMARC version (v=DMARC1)
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Policy for the domain (p=none/quarantine/reject)
        /// </summary>
        public DmarcPolicy Policy { get; set; } = DmarcPolicy.None;

        /// <summary>
        /// Policy for subdomains (sp=none/quarantine/reject)
        /// </summary>
        public DmarcPolicy? SubdomainPolicy { get; set; }

        /// <summary>
        /// Percentage of messages to apply policy to (pct=100)
        /// </summary>
        public int Percentage { get; set; } = 100;

        /// <summary>
        /// Alignment mode for SPF (aspf=r/s)
        /// </summary>
        public DmarcAlignment SpfAlignment { get; set; } = DmarcAlignment.Relaxed;

        /// <summary>
        /// Alignment mode for DKIM (adkim=r/s)
        /// </summary>
        public DmarcAlignment DkimAlignment { get; set; } = DmarcAlignment.Relaxed;

        /// <summary>
        /// Reporting URI for aggregate reports (rua=mailto:...)
        /// </summary>
        public List<string> AggregateReportUris { get; set; } = [];

        /// <summary>
        /// Reporting URI for forensic reports (ruf=mailto:...)
        /// </summary>
        public List<string> ForensicReportUris { get; set; } = [];

        /// <summary>
        /// Reporting interval in seconds (ri=86400)
        /// </summary>
        public int? ReportingInterval { get; set; }

        /// <summary>
        /// Failure reporting options (fo=0/1/d/s)
        /// </summary>
        public string? FailureReportingOptions { get; set; }

        /// <summary>
        /// Parse a DMARC record from DNS TXT record
        /// </summary>
        public static DmarcRecord? Parse(string recordValue)
        {
            if (string.IsNullOrWhiteSpace(recordValue))
            {
                return null;
            }

            DmarcRecord record = new();
            Dictionary<string, string> tags = ParseTags(recordValue);

            foreach (KeyValuePair<string, string> tag in tags)
            {
                switch (tag.Key.ToLowerInvariant())
                {
                    case "v":
                        record.Version = tag.Value;
                        break;

                    case "p":
                        record.Policy = ParsePolicy(tag.Value);
                        break;

                    case "sp":
                        record.SubdomainPolicy = ParsePolicy(tag.Value);
                        break;

                    case "pct":
                        if (int.TryParse(tag.Value, out int pct))
                        {
                            record.Percentage = Math.Max(0, Math.Min(100, pct));
                        }

                        break;

                    case "aspf":
                        record.SpfAlignment = ParseAlignment(tag.Value);
                        break;

                    case "adkim":
                        record.DkimAlignment = ParseAlignment(tag.Value);
                        break;

                    case "rua":
                        record.AggregateReportUris = ParseUris(tag.Value);
                        break;

                    case "ruf":
                        record.ForensicReportUris = ParseUris(tag.Value);
                        break;

                    case "ri":
                        if (int.TryParse(tag.Value, out int ri))
                        {
                            record.ReportingInterval = ri;
                        }

                        break;

                    case "fo":
                        record.FailureReportingOptions = tag.Value;
                        break;
                }
            }

            // Validate required fields
            if (record.Version != "DMARC1")
            {
                return null;
            }

            return record;
        }

        private static Dictionary<string, string> ParseTags(string recordValue)
        {
            Dictionary<string, string> tags = [];

            // Split by semicolon and parse key=value pairs
            var parts = recordValue.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                var equalIndex = trimmed.IndexOf('=');

                if (equalIndex > 0)
                {
                    var key = trimmed[..equalIndex].Trim();
                    var value = trimmed[(equalIndex + 1)..].Trim();
                    tags[key] = value;
                }
            }

            return tags;
        }

        private static DmarcPolicy ParsePolicy(string value)
        {
            return value?.ToLowerInvariant() switch
            {
                "none" => DmarcPolicy.None,
                "quarantine" => DmarcPolicy.Quarantine,
                "reject" => DmarcPolicy.Reject,
                _ => DmarcPolicy.None
            };
        }

        private static DmarcAlignment ParseAlignment(string value)
        {
            return value?.ToLowerInvariant() switch
            {
                "s" or "strict" => DmarcAlignment.Strict,
                "r" or "relaxed" => DmarcAlignment.Relaxed,
                _ => DmarcAlignment.Relaxed
            };
        }

        private static List<string> ParseUris(string value)
        {
            return [.. value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                     .Select(u => u.Trim())];
        }

        /// <summary>
        /// Determines if the policy should be applied based on percentage
        /// </summary>
        public bool ShouldApplyPolicy()
        {
            if (Percentage >= 100)
            {
                return true;
            }

            if (Percentage <= 0)
            {
                return false;
            }

            Random random = new();
            return random.Next(100) < Percentage;
        }

        /// <summary>
        /// Gets the effective policy for a given domain
        /// </summary>
        public DmarcPolicy GetEffectivePolicy(string domain, string organizationalDomain)
        {
            // If it's a subdomain and subdomain policy is set, use that
            if (SubdomainPolicy.HasValue &&
                !domain.Equals(organizationalDomain, StringComparison.OrdinalIgnoreCase))
            {
                return SubdomainPolicy.Value;
            }

            return Policy;
        }
    }
}