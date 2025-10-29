using System.Threading;
using System.Threading.Tasks;
using Zetian.AntiSpam.Models;

namespace Zetian.AntiSpam.Abstractions
{
    /// <summary>
    /// Defines the contract for spam checking implementations
    /// </summary>
    public interface ISpamChecker
    {
        /// <summary>
        /// Gets the name of the spam checker
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the weight of this checker in the overall spam score calculation
        /// </summary>
        double Weight { get; }

        /// <summary>
        /// Gets whether this checker is enabled
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Checks if the email is spam
        /// </summary>
        /// <param name="context">The spam check context containing email details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The spam check result</returns>
        Task<SpamCheckResult> CheckAsync(SpamCheckContext context, CancellationToken cancellationToken = default);
    }
}