using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Core;

namespace Zetian.Storage
{
    /// <summary>
    /// Filters mailboxes based on allowed/blocked domains
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of DomainMailboxFilter
    /// </remarks>
    /// <param name="allowByDefault">Whether to allow domains by default if not in any list</param>
    public class DomainMailboxFilter(bool allowByDefault = true) : IMailboxFilter
    {
        private readonly HashSet<string> _allowedFromDomains = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _blockedFromDomains = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _allowedToDomains = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _blockedToDomains = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Add allowed sender domains
        /// </summary>
        public DomainMailboxFilter AllowFromDomains(params string[] domains)
        {
            foreach (string domain in domains)
            {
                _allowedFromDomains.Add(domain.ToLowerInvariant());
            }
            return this;
        }

        /// <summary>
        /// Add blocked sender domains
        /// </summary>
        public DomainMailboxFilter BlockFromDomains(params string[] domains)
        {
            foreach (string domain in domains)
            {
                _blockedFromDomains.Add(domain.ToLowerInvariant());
            }
            return this;
        }

        /// <summary>
        /// Add allowed recipient domains
        /// </summary>
        public DomainMailboxFilter AllowToDomains(params string[] domains)
        {
            foreach (string domain in domains)
            {
                _allowedToDomains.Add(domain.ToLowerInvariant());
            }
            return this;
        }

        /// <summary>
        /// Add blocked recipient domains
        /// </summary>
        public DomainMailboxFilter BlockToDomains(params string[] domains)
        {
            foreach (string domain in domains)
            {
                _blockedToDomains.Add(domain.ToLowerInvariant());
            }
            return this;
        }

        /// <inheritdoc />
        public Task<bool> CanAcceptFromAsync(
            ISmtpSession session,
            string from,
            long size,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(from))
            {
                // Accept null sender (bounce messages)
                return Task.FromResult(true);
            }

            string domain = GetDomain(from);
            if (string.IsNullOrEmpty(domain))
            {
                return Task.FromResult(allowByDefault);
            }

            // Check blocked list first
            if (_blockedFromDomains.Contains(domain))
            {
                return Task.FromResult(false);
            }

            // Check allowed list
            if (_allowedFromDomains.Count > 0)
            {
                // If domain is in whitelist, accept
                if (_allowedFromDomains.Contains(domain))
                {
                    return Task.FromResult(true);
                }

                // If we have ONLY whitelist (no blacklist), then whitelist is exclusive
                if (_blockedFromDomains.Count == 0)
                {
                    return Task.FromResult(false);
                }
            }

            // No specific rules or mixed mode with domain not in any list, use default
            return Task.FromResult(allowByDefault);
        }

        /// <inheritdoc />
        public Task<bool> CanDeliverToAsync(
            ISmtpSession session,
            string to,
            string from,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(to))
            {
                return Task.FromResult(false);
            }

            string domain = GetDomain(to);
            if (string.IsNullOrEmpty(domain))
            {
                return Task.FromResult(allowByDefault);
            }

            // Check blocked list first
            if (_blockedToDomains.Contains(domain))
            {
                return Task.FromResult(false);
            }

            // Check allowed list
            if (_allowedToDomains.Count > 0)
            {
                // If domain is in whitelist, accept
                if (_allowedToDomains.Contains(domain))
                {
                    return Task.FromResult(true);
                }

                // If we have ONLY whitelist (no blacklist), then whitelist is exclusive
                if (_blockedToDomains.Count == 0)
                {
                    return Task.FromResult(false);
                }
            }

            // No specific rules or mixed mode with domain not in any list, use default
            return Task.FromResult(allowByDefault);
        }

        private static string GetDomain(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return string.Empty;
            }

            int atIndex = email.LastIndexOf('@');
            if (atIndex < 0 || atIndex == email.Length - 1)
            {
                return string.Empty;
            }

            return email[(atIndex + 1)..].ToLowerInvariant();
        }
    }
}