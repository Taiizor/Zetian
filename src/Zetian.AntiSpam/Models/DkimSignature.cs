using System;
using System.Collections.Generic;
using System.Linq;

namespace Zetian.AntiSpam.Models
{
    /// <summary>
    /// Represents a parsed DKIM signature
    /// </summary>
    public class DkimSignature
    {
        /// <summary>
        /// Version (v=1)
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Signature algorithm (a=rsa-sha256)
        /// </summary>
        public string? Algorithm { get; set; }

        /// <summary>
        /// Canonicalization method (c=relaxed/relaxed)
        /// </summary>
        public string? Canonicalization { get; set; }

        /// <summary>
        /// Signing domain (d=example.com)
        /// </summary>
        public string? Domain { get; set; }

        /// <summary>
        /// Selector (s=selector)
        /// </summary>
        public string? Selector { get; set; }

        /// <summary>
        /// Signed headers (h=from:to:subject)
        /// </summary>
        public List<string> SignedHeaders { get; set; } = [];

        /// <summary>
        /// Body hash (bh=...)
        /// </summary>
        public string? BodyHash { get; set; }

        /// <summary>
        /// Signature value (b=...)
        /// </summary>
        public string? SignatureValue { get; set; }

        /// <summary>
        /// Signature timestamp (t=...)
        /// </summary>
        public long? Timestamp { get; set; }

        /// <summary>
        /// Signature expiration (x=...)
        /// </summary>
        public long? Expiration { get; set; }

        /// <summary>
        /// Body length limit (l=...)
        /// </summary>
        public int? BodyLength { get; set; }

        /// <summary>
        /// Query methods (q=dns/txt)
        /// </summary>
        public string? QueryMethods { get; set; }

        /// <summary>
        /// Identity (i=user@example.com)
        /// </summary>
        public string? Identity { get; set; }

        /// <summary>
        /// Checks if the signature has expired
        /// </summary>
        public bool IsExpired()
        {
            if (Expiration == null)
            {
                return false;
            }

            return DateTimeOffset.FromUnixTimeSeconds(Expiration.Value) < DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets the signature age
        /// </summary>
        public TimeSpan? GetAge()
        {
            if (Timestamp == null)
            {
                return null;
            }

            return DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(Timestamp.Value);
        }

        /// <summary>
        /// Parse a DKIM-Signature header value
        /// </summary>
        public static DkimSignature? Parse(string headerValue)
        {
            if (string.IsNullOrWhiteSpace(headerValue))
            {
                return null;
            }

            DkimSignature signature = new();
            Dictionary<string, string> tags = ParseTags(headerValue);

            foreach (KeyValuePair<string, string> tag in tags)
            {
                switch (tag.Key.ToLowerInvariant())
                {
                    case "v":
                        signature.Version = tag.Value;
                        break;
                    case "a":
                        signature.Algorithm = tag.Value;
                        break;
                    case "c":
                        signature.Canonicalization = tag.Value;
                        break;
                    case "d":
                        signature.Domain = tag.Value;
                        break;
                    case "s":
                        signature.Selector = tag.Value;
                        break;
                    case "h":
                        signature.SignedHeaders = ParseHeaders(tag.Value);
                        break;
                    case "bh":
                        signature.BodyHash = tag.Value;
                        break;
                    case "b":
                        signature.SignatureValue = tag.Value;
                        break;
                    case "t":
                        if (long.TryParse(tag.Value, out long timestamp))
                        {
                            signature.Timestamp = timestamp;
                        }

                        break;
                    case "x":
                        if (long.TryParse(tag.Value, out long expiration))
                        {
                            signature.Expiration = expiration;
                        }

                        break;
                    case "l":
                        if (int.TryParse(tag.Value, out int length))
                        {
                            signature.BodyLength = length;
                        }

                        break;
                    case "q":
                        signature.QueryMethods = tag.Value;
                        break;
                    case "i":
                        signature.Identity = tag.Value;
                        break;
                }
            }

            return signature;
        }

        private static Dictionary<string, string> ParseTags(string headerValue)
        {
            Dictionary<string, string> tags = [];

            // Remove whitespace and split by semicolon
            var parts = headerValue.Replace("\r\n", "")
                                 .Replace("\n", "")
                                 .Replace("\t", " ")
                                 .Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var equalIndex = part.IndexOf('=');
                if (equalIndex > 0)
                {
                    var key = part[..equalIndex].Trim();
                    var value = part[(equalIndex + 1)..].Trim();
                    tags[key] = value;
                }
            }

            return tags;
        }

        private static List<string> ParseHeaders(string headerList)
        {
            return [.. headerList.Split(':', StringSplitOptions.RemoveEmptyEntries)
                         .Select(h => h.Trim().ToLowerInvariant())];
        }
    }
}