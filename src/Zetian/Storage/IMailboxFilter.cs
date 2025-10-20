using System.Threading;
using System.Threading.Tasks;
using Zetian.Core;

namespace Zetian.Storage
{
    /// <summary>
    /// Interface for filtering mailboxes (senders and recipients)
    /// </summary>
    public interface IMailboxFilter
    {
        /// <summary>
        /// Returns a value indicating whether the given sender can be accepted
        /// </summary>
        /// <param name="session">The session context</param>
        /// <param name="from">The sender's email address</param>
        /// <param name="size">The estimated message size in bytes</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>True if the sender is accepted, false if not</returns>
        Task<bool> CanAcceptFromAsync(
            ISmtpSession session,
            string from,
            long size,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a value indicating whether the given recipient can be accepted
        /// </summary>
        /// <param name="session">The session context</param>
        /// <param name="to">The recipient's email address</param>
        /// <param name="from">The sender's email address</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>True if the recipient can be delivered to, false if not</returns>
        Task<bool> CanDeliverToAsync(
            ISmtpSession session,
            string to,
            string from,
            CancellationToken cancellationToken = default);
    }
}
