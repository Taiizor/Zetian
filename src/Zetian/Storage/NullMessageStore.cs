using System.Threading;
using System.Threading.Tasks;
using Zetian.Core;

namespace Zetian.Storage
{
    /// <summary>
    /// A message store that does nothing (null object pattern)
    /// </summary>
    public class NullMessageStore : IMessageStore
    {
        /// <summary>
        /// Gets the default instance
        /// </summary>
        public static readonly NullMessageStore Instance = new();

        /// <inheritdoc />
        public Task<bool> SaveAsync(ISmtpSession session, ISmtpMessage message, CancellationToken cancellationToken = default)
        {
            // Do nothing, just return success
            return Task.FromResult(true);
        }
    }
}