using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Core;

namespace Zetian.Storage
{
    /// <summary>
    /// Combines multiple mailbox filters into one
    /// </summary>
    public class CompositeMailboxFilter : IMailboxFilter
    {
        private readonly List<IMailboxFilter> _filters;
        private readonly CompositeMode _mode;

        /// <summary>
        /// Composite filter mode
        /// </summary>
        public enum CompositeMode
        {
            /// <summary>
            /// All filters must accept (AND logic)
            /// </summary>
            All,

            /// <summary>
            /// At least one filter must accept (OR logic)
            /// </summary>
            Any
        }

        /// <summary>
        /// Initializes a new instance of CompositeMailboxFilter
        /// </summary>
        /// <param name="mode">The composite mode (All or Any)</param>
        /// <param name="filters">Initial filters to add</param>
        public CompositeMailboxFilter(CompositeMode mode = CompositeMode.All, params IMailboxFilter[] filters)
        {
            _mode = mode;
            _filters = new List<IMailboxFilter>(filters ?? Array.Empty<IMailboxFilter>());
        }

        /// <summary>
        /// Add a filter to the composite
        /// </summary>
        public CompositeMailboxFilter AddFilter(IMailboxFilter filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

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

            return _mode == CompositeMode.All
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

            return _mode == CompositeMode.All
                ? results.All(r => r)
                : results.Any(r => r);
        }
    }
}