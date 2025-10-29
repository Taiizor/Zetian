using DnsClient;
using DnsClient.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.AntiSpam.Abstractions;
using Zetian.AntiSpam.Enums;
using Zetian.AntiSpam.Models;

namespace Zetian.AntiSpam.Checkers
{
    /// <summary>
    /// Checks DKIM (DomainKeys Identified Mail) signatures
    /// </summary>
    public class DkimChecker : ISpamChecker
    {
        private readonly ILookupClient _dnsClient;
        private readonly double _failScore;
        private readonly double _noneScore;
        private readonly bool _strictMode;
        private readonly TimeSpan _maxSignatureAge;

        public DkimChecker(
            ILookupClient? dnsClient = null,
            double failScore = 40,
            double noneScore = 10,
            bool strictMode = false,
            TimeSpan? maxSignatureAge = null)
        {
            _dnsClient = dnsClient ?? new LookupClient();
            _failScore = failScore;
            _noneScore = noneScore;
            _strictMode = strictMode;
            _maxSignatureAge = maxSignatureAge ?? TimeSpan.FromDays(7);
            IsEnabled = true;
        }

        public string Name => "DKIM";

        public bool IsEnabled { get; set; }

        public async Task<SpamCheckResult> CheckAsync(
            ISmtpMessage message,
            ISmtpSession session,
            CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
            {
                return SpamCheckResult.Clean(0, "DKIM check disabled");
            }

            try
            {
                // Find DKIM-Signature header
                if (!message.Headers.TryGetValue("DKIM-Signature", out string? signatureHeader) ||
                    string.IsNullOrWhiteSpace(signatureHeader))
                {
                    return SpamCheckResult.Clean(_noneScore, "No DKIM signature found");
                }

                // Parse DKIM signature
                DkimSignature? signature = DkimSignature.Parse(signatureHeader);
                if (signature == null)
                {
                    return SpamCheckResult.Spam(_failScore, "Invalid DKIM signature format");
                }

                // Validate signature fields
                string? validationError = ValidateSignature(signature);
                if (validationError != null)
                {
                    return SpamCheckResult.Spam(_failScore, $"DKIM validation failed: {validationError}");
                }

                // Check signature expiration
                if (signature.IsExpired())
                {
                    return SpamCheckResult.Spam(_failScore, "DKIM signature expired");
                }

                // Check signature age
                TimeSpan? age = signature.GetAge();
                if (age.HasValue && age.Value > _maxSignatureAge)
                {
                    return SpamCheckResult.Spam(_failScore / 2, $"DKIM signature too old: {age.Value.TotalDays:F1} days");
                }

                // Get public key from DNS
                DkimPublicKey? publicKey = await GetPublicKeyAsync(signature.Domain!, signature.Selector!, cancellationToken);
                if (publicKey == null)
                {
                    return SpamCheckResult.Spam(_failScore, "DKIM public key not found in DNS");
                }

                // Verify signature
                DkimResult result = await VerifySignatureAsync(message, signature, publicKey, cancellationToken);

                return result switch
                {
                    DkimResult.Pass => SpamCheckResult.Clean(0, $"DKIM signature valid for {signature.Domain}"),
                    DkimResult.Fail => SpamCheckResult.Spam(_failScore, "DKIM signature verification failed"),
                    DkimResult.Policy => SpamCheckResult.Spam(_failScore / 2, "DKIM policy violation"),
                    _ => SpamCheckResult.Clean(_noneScore, $"DKIM check inconclusive: {result}")
                };
            }
            catch (Exception ex)
            {
                return SpamCheckResult.Clean(0, $"DKIM check error: {ex.Message}");
            }
        }

        private string? ValidateSignature(DkimSignature signature)
        {
            if (signature.Version != "1")
            {
                return "Unsupported DKIM version";
            }

            if (string.IsNullOrWhiteSpace(signature.Domain))
            {
                return "Missing domain (d=) tag";
            }

            if (string.IsNullOrWhiteSpace(signature.Selector))
            {
                return "Missing selector (s=) tag";
            }

            if (string.IsNullOrWhiteSpace(signature.BodyHash))
            {
                return "Missing body hash (bh=) tag";
            }

            if (string.IsNullOrWhiteSpace(signature.SignatureValue))
            {
                return "Missing signature (b=) tag";
            }

            if (!signature.SignedHeaders.Any())
            {
                return "No signed headers specified";
            }

            // Check required headers are signed
            string[] requiredHeaders = new[] { "from" };
            if (_strictMode)
            {
                requiredHeaders = new[] { "from", "to", "subject", "date" };
            }

            foreach (string? required in requiredHeaders)
            {
                if (!signature.SignedHeaders.Contains(required))
                {
                    return $"Required header '{required}' not signed";
                }
            }

            // Validate algorithm
            if (!IsValidAlgorithm(signature.Algorithm))
            {
                return $"Unsupported algorithm: {signature.Algorithm}";
            }

            return null;
        }

        private bool IsValidAlgorithm(string? algorithm)
        {
            if (algorithm == null)
            {
                return false;
            }

            string[] supported = new[]
            {
                "rsa-sha1", "rsa-sha256",
                "ed25519-sha256"
            };

            return supported.Contains(algorithm.ToLowerInvariant());
        }

        private async Task<DkimPublicKey?> GetPublicKeyAsync(
            string domain,
            string selector,
            CancellationToken cancellationToken)
        {
            try
            {
                // Query DNS for DKIM public key
                string query = $"{selector}._domainkey.{domain}";
                IDnsQueryResponse result = await _dnsClient.QueryAsync(query, QueryType.TXT, cancellationToken: cancellationToken);

                TxtRecord? txtRecord = result.Answers
                    .OfType<DnsClient.Protocol.TxtRecord>()
                    .FirstOrDefault();

                if (txtRecord == null)
                {
                    return null;
                }

                // Parse DKIM public key record
                string keyRecord = string.Join("", txtRecord.Text);
                return DkimPublicKey.Parse(keyRecord);
            }
            catch
            {
                return null;
            }
        }

        private async Task<DkimResult> VerifySignatureAsync(
            ISmtpMessage message,
            DkimSignature signature,
            DkimPublicKey publicKey,
            CancellationToken cancellationToken)
        {
            try
            {
                // Verify body hash
                string bodyHash = ComputeBodyHash(message, signature);
                if (bodyHash != signature.BodyHash)
                {
                    return DkimResult.Fail;
                }

                // Build headers to sign
                string headersToSign = BuildHeadersForSigning(message, signature);

                // Compute signature
                string computedSignature = ComputeSignature(headersToSign, signature, publicKey);

                // Compare signatures
                if (computedSignature == signature.SignatureValue?.Replace(" ", ""))
                {
                    return DkimResult.Pass;
                }

                return DkimResult.Fail;
            }
            catch (Exception)
            {
                return DkimResult.TempError;
            }
        }

        private string ComputeBodyHash(ISmtpMessage message, DkimSignature signature)
        {
            // Get canonicalization method
            (string header, string body) canonicalization = ParseCanonicalization(signature.Canonicalization);

            // Canonicalize body
            string body = message.TextBody ?? message.HtmlBody ?? "";
            if (signature.BodyLength.HasValue && signature.BodyLength.Value < body.Length)
            {
                body = body[..signature.BodyLength.Value];
            }

            body = CanonicalizeBody(body, canonicalization.body);

            // Compute hash
            using HashAlgorithm hasher = GetHashAlgorithm(signature.Algorithm);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            byte[] hashBytes = hasher.ComputeHash(bodyBytes);
            return Convert.ToBase64String(hashBytes);
        }

        private string BuildHeadersForSigning(ISmtpMessage message, DkimSignature signature)
        {
            StringBuilder headers = new();

            foreach (string headerName in signature.SignedHeaders)
            {
                if (message.Headers.TryGetValue(headerName, out string? value))
                {
                    headers.AppendLine($"{headerName}: {value}");
                }
            }

            // Add DKIM-Signature header (without b= value)
            if (message.Headers.TryGetValue("DKIM-Signature", out string? dkimHeader))
            {
                // Remove b= value for signing
                string signatureForSigning = System.Text.RegularExpressions.Regex.Replace(
                    dkimHeader,
                    @"b=([^;]+)",
                    "b=");
                headers.Append($"dkim-signature: {signatureForSigning}");
            }

            return headers.ToString();
        }

        private string ComputeSignature(string headers, DkimSignature signature, DkimPublicKey publicKey)
        {
            // This is a simplified implementation
            // In production, you would use proper RSA or Ed25519 verification
            using HashAlgorithm hasher = GetHashAlgorithm(signature.Algorithm);
            byte[] headerBytes = Encoding.UTF8.GetBytes(headers);
            byte[] hashBytes = hasher.ComputeHash(headerBytes);

            // For demonstration - actual implementation would verify RSA/Ed25519 signature
            return Convert.ToBase64String(hashBytes);
        }

        private HashAlgorithm GetHashAlgorithm(string? algorithm)
        {
            return algorithm?.ToLowerInvariant() switch
            {
                "rsa-sha256" or "ed25519-sha256" => SHA256.Create(),
                "rsa-sha1" => SHA1.Create(),
                _ => SHA256.Create()
            };
        }

        private string CanonicalizeBody(string body, string method)
        {
            if (method == "simple")
            {
                // Remove trailing empty lines
                return body.TrimEnd('\r', '\n') + "\r\n";
            }
            else // relaxed
            {
                // Convert all whitespace to single spaces
                // Remove trailing whitespace on lines
                // Remove trailing empty lines
                string[] lines = body.Split('\n');
                List<string> canonicalized = [];

                foreach (string line in lines)
                {
                    string trimmed = line.TrimEnd();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        // Replace multiple spaces with single space
                        trimmed = System.Text.RegularExpressions.Regex.Replace(trimmed, @"\s+", " ");
                        canonicalized.Add(trimmed);
                    }
                }

                return string.Join("\r\n", canonicalized) + "\r\n";
            }
        }

        private (string header, string body) ParseCanonicalization(string? canonicalization)
        {
            if (string.IsNullOrWhiteSpace(canonicalization))
            {
                return ("simple", "simple");
            }

            string[] parts = canonicalization.Split('/');
            if (parts.Length == 1)
            {
                return (parts[0], "simple");
            }

            return (parts[0], parts[1]);
        }
    }

    /// <summary>
    /// Represents a DKIM public key from DNS
    /// </summary>
    public class DkimPublicKey
    {
        public string? Version { get; set; }
        public string? KeyType { get; set; }
        public string? PublicKey { get; set; }
        public string? ServiceType { get; set; }
        public string? Flags { get; set; }
        public string? Notes { get; set; }
        public List<string> AcceptableHashAlgorithms { get; set; } = [];

        public static DkimPublicKey? Parse(string recordValue)
        {
            if (string.IsNullOrWhiteSpace(recordValue))
            {
                return null;
            }

            DkimPublicKey key = new();
            Dictionary<string, string> tags = ParseTags(recordValue);

            foreach (KeyValuePair<string, string> tag in tags)
            {
                switch (tag.Key.ToLowerInvariant())
                {
                    case "v":
                        key.Version = tag.Value;
                        break;
                    case "k":
                        key.KeyType = tag.Value;
                        break;
                    case "p":
                        key.PublicKey = tag.Value;
                        break;
                    case "s":
                        key.ServiceType = tag.Value;
                        break;
                    case "t":
                        key.Flags = tag.Value;
                        break;
                    case "n":
                        key.Notes = tag.Value;
                        break;
                    case "h":
                        key.AcceptableHashAlgorithms = tag.Value.Split(':', StringSplitOptions.RemoveEmptyEntries).ToList();
                        break;
                }
            }

            // Empty public key means revoked
            if (string.IsNullOrWhiteSpace(key.PublicKey))
            {
                return null;
            }

            return key;
        }

        private static Dictionary<string, string> ParseTags(string recordValue)
        {
            Dictionary<string, string> tags = [];
            string[] parts = recordValue.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                int equalIndex = part.IndexOf('=');
                if (equalIndex > 0)
                {
                    string tagKey = part[..equalIndex].Trim();
                    string tagValue = part[(equalIndex + 1)..].Trim();
                    tags[tagKey] = tagValue;
                }
            }

            return tags;
        }
    }
}