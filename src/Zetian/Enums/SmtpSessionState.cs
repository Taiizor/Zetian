namespace Zetian.Enums
{
    /// <summary>
    /// Represents the various states of an SMTP session.
    /// </summary>
    internal enum SmtpSessionState
    {
        Connected,
        Hello,
        Mail,
        Recipient,
        Data,
        Quit
    }
}