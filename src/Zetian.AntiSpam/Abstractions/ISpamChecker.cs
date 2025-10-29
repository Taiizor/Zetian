using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.AntiSpam.Models;

namespace Zetian.AntiSpam.Abstractions
{
    /// <summary>
    /// Interface for spam checking implementations
    /// </summary>
    public interface ISpamChecker
    {
        /// <summary>
        /// Gets the name of the spam checker
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets or sets whether this checker is enabled
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Checks if a message is spam
        /// </summary>
        /// <param name="message">The message to check</param>
        /// <param name="session">The SMTP session</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The spam check result</returns>
        Task<SpamCheckResult> CheckAsync(
            ISmtpMessage message,
            ISmtpSession session,
            CancellationToken cancellationToken = default);
    }
}