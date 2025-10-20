using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Zetian.Core
{
    /// <summary>
    /// Represents an SMTP message
    /// </summary>
    public interface ISmtpMessage
    {
        /// <summary>
        /// Gets the message ID
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the sender address (MAIL FROM)
        /// </summary>
        MailAddress? From { get; }

        /// <summary>
        /// Gets the recipient addresses (RCPT TO)
        /// </summary>
        IReadOnlyList<MailAddress> Recipients { get; }

        /// <summary>
        /// Gets the message size in bytes
        /// </summary>
        long Size { get; }

        /// <summary>
        /// Gets the raw message data stream
        /// </summary>
        Stream GetRawDataStream();

        /// <summary>
        /// Gets the raw message data as byte array
        /// </summary>
        byte[] GetRawData();

        /// <summary>
        /// Asynchronously gets the raw message data as byte array
        /// </summary>
        Task<byte[]> GetRawDataAsync();

        /// <summary>
        /// Gets the message headers
        /// </summary>
        IDictionary<string, string> Headers { get; }

        /// <summary>
        /// Gets a specific header value
        /// </summary>
        string? GetHeader(string name);

        /// <summary>
        /// Gets all values for a specific header
        /// </summary>
        IEnumerable<string> GetHeaders(string name);

        /// <summary>
        /// Gets the message subject
        /// </summary>
        string? Subject { get; }

        /// <summary>
        /// Gets the message body as text
        /// </summary>
        string? TextBody { get; }

        /// <summary>
        /// Gets the message body as HTML
        /// </summary>
        string? HtmlBody { get; }

        /// <summary>
        /// Gets whether the message has attachments
        /// </summary>
        bool HasAttachments { get; }

        /// <summary>
        /// Gets the attachment count
        /// </summary>
        int AttachmentCount { get; }

        /// <summary>
        /// Gets the message date
        /// </summary>
        DateTime? Date { get; }

        /// <summary>
        /// Gets the message priority
        /// </summary>
        MailPriority Priority { get; }

        /// <summary>
        /// Saves the message to a file
        /// </summary>
        void SaveToFile(string path);

        /// <summary>
        /// Asynchronously saves the message to a file
        /// </summary>
        Task SaveToFileAsync(string path);

        /// <summary>
        /// Saves the message to a stream
        /// </summary>
        void SaveToStream(Stream stream);

        /// <summary>
        /// Asynchronously saves the message to a stream
        /// </summary>
        Task SaveToStreamAsync(Stream stream);
    }
}