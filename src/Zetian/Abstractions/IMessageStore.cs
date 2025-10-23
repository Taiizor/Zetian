using System.Threading;
using System.Threading.Tasks;

namespace Zetian.Abstractions
{
    /// <summary>
    /// Interface for storing SMTP messages
    /// </summary>
    public interface IMessageStore
    {
        /// <summary>
        /// Save the given message to the underlying storage system
        /// </summary>
        /// <param name="session">The SMTP session</param>
        /// <param name="message">The message to store</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>True if the message was saved successfully, false otherwise</returns>
        Task<bool> SaveAsync(ISmtpSession session, ISmtpMessage message, CancellationToken cancellationToken = default);
    }
}