using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Core;

namespace Zetian.Storage
{
    /// <summary>
    /// Stores messages as .eml files in a directory
    /// </summary>
    public class FileMessageStore : IMessageStore
    {
        private readonly string _directory;
        private readonly bool _createDirectoryStructure;

        /// <summary>
        /// Initializes a new instance of FileMessageStore
        /// </summary>
        /// <param name="directory">The directory to store messages in</param>
        /// <param name="createDirectoryStructure">Whether to create date-based subdirectories</param>
        public FileMessageStore(string directory, bool createDirectoryStructure = true)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
            _createDirectoryStructure = createDirectoryStructure;

            if (!Directory.Exists(_directory))
            {
                Directory.CreateDirectory(_directory);
            }
        }

        /// <inheritdoc />
        public async Task<bool> SaveAsync(ISmtpSession session, ISmtpMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                string targetDir = _directory;

                if (_createDirectoryStructure)
                {
                    // Create directory structure: year/month/day
                    DateTime now = DateTime.Now;
                    targetDir = Path.Combine(_directory, now.ToString("yyyy"), now.ToString("MM"), now.ToString("dd"));

                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }
                }

                // Generate filename: timestamp_messageId_sessionId.eml
                string filename = $"{DateTime.Now:yyyyMMdd_HHmmss}_{message.Id}_{session.Id}.eml";
                string filePath = Path.Combine(targetDir, filename);

                await message.SaveToFileAsync(filePath).ConfigureAwait(false);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
