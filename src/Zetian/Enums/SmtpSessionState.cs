namespace Zetian.Enums
{
    /// <summary>
    /// Represents the various states of an SMTP session.
    /// </summary>
    internal enum SmtpSessionState
    {
        /// <summary>
        /// Gets a value indicating whether the connection to the resource is currently established.
        /// </summary>
        Connected,

        /// <summary>
        /// Represents a greeting or salutation.
        /// </summary>
        Hello,

        /// <summary>
        /// Represents an email message, including its content, recipients, and related metadata.
        /// </summary>
        Mail,

        /// <summary>
        /// Represents a recipient of a message, notification, or other communication.
        /// </summary>
        Recipient,

        /// <summary>
        /// Gets or sets the data associated with this instance.
        /// </summary>
        Data,

        /// <summary>
        /// Represents an action or command to exit or terminate the current process or application.
        /// </summary>
        Quit
    }
}