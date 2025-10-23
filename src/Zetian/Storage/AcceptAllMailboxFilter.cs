using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;

namespace Zetian.Storage
{
    /// <summary>
    /// A mailbox filter that accepts all senders and recipients
    /// </summary>
    public class AcceptAllMailboxFilter : IMailboxFilter
    {
        /// <summary>
        /// Gets the default instance
        /// </summary>
        public static readonly AcceptAllMailboxFilter Instance = new();

        /// <inheritdoc />
        public Task<bool> CanAcceptFromAsync(
            ISmtpSession session,
            string from,
            long size,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public Task<bool> CanDeliverToAsync(
            ISmtpSession session,
            string to,
            string from,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}