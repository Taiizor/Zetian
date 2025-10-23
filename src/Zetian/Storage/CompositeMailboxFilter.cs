using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.Enums;

namespace Zetian.Storage
{
    /// <summary>
    /// Combines multiple mailbox filters into one
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of CompositeMailboxFilter
    /// </remarks>
    /// <param name="mode">The composite mode (All or Any)</param>
    /// <param name="filters">Initial filters to add</param>
    public class CompositeMailboxFilter(CompositeMode mode = CompositeMode.All, params IMailboxFilter[] filters) : IMailboxFilter
    {
        private readonly List<IMailboxFilter> _filters = new(filters ?? Array.Empty<IMailboxFilter>());

        /// <summary>
        /// Add a filter to the composite
        /// </summary>
        public CompositeMailboxFilter AddFilter(IMailboxFilter filter)
        {
            ArgumentNullException.ThrowIfNull(filter);

            _filters.Add(filter);
            return this;
        }

        /// <summary>
        /// Remove a filter from the composite
        /// </summary>
        public CompositeMailboxFilter RemoveFilter(IMailboxFilter filter)
        {
            _filters.Remove(filter);
            return this;
        }

        /// <inheritdoc />
        public async Task<bool> CanAcceptFromAsync(
            ISmtpSession session,
            string from,
            long size,
            CancellationToken cancellationToken = default)
        {
            if (_filters.Count == 0)
            {
                return true;
            }

            IEnumerable<Task<bool>> tasks = _filters.Select(f => f.CanAcceptFromAsync(session, from, size, cancellationToken));
            bool[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

            return mode == CompositeMode.All
                ? results.All(r => r)
                : results.Any(r => r);
        }

        /// <inheritdoc />
        public async Task<bool> CanDeliverToAsync(
            ISmtpSession session,
            string to,
            string from,
            CancellationToken cancellationToken = default)
        {
            if (_filters.Count == 0)
            {
                return true;
            }

            IEnumerable<Task<bool>> tasks = _filters.Select(f => f.CanDeliverToAsync(session, to, from, cancellationToken));
            bool[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

            return mode == CompositeMode.All
                ? results.All(r => r)
                : results.Any(r => r);
        }
    }
}