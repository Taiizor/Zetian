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
    public class DomainMailboxFilter : IMailboxFilter
    {
        private readonly HashSet<string> _allowedFromDomains;
        private readonly HashSet<string> _blockedFromDomains;
        private readonly HashSet<string> _allowedToDomains;
        private readonly HashSet<string> _blockedToDomains;
        private readonly bool _allowByDefault;

        /// <summary>
        /// Initializes a new instance of DomainMailboxFilter
        /// </summary>
        /// <param name="allowByDefault">Whether to allow domains by default if not in any list</param>
        public DomainMailboxFilter(bool allowByDefault = true)
        {
            _allowByDefault = allowByDefault;
            _allowedFromDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _blockedFromDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _allowedToDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _blockedToDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

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
                return Task.FromResult(_allowByDefault);
            }

            // Check blocked list first
            if (_blockedFromDomains.Contains(domain))
            {
                return Task.FromResult(false);
            }

            // If we have an allowed list and the domain is in it, accept
            if (_allowedFromDomains.Count > 0)
            {
                return Task.FromResult(_allowedFromDomains.Contains(domain));
            }

            // No specific rules, use default
            return Task.FromResult(_allowByDefault);
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
                return Task.FromResult(_allowByDefault);
            }

            // Check blocked list first
            if (_blockedToDomains.Contains(domain))
            {
                return Task.FromResult(false);
            }

            // If we have an allowed list and the domain is in it, accept
            if (_allowedToDomains.Count > 0)
            {
                return Task.FromResult(_allowedToDomains.Contains(domain));
            }

            // No specific rules, use default
            return Task.FromResult(_allowByDefault);
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
