using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zetian.Protocol
{
    /// <summary>
    /// Represents an SMTP response
    /// </summary>
    public class SmtpResponse
    {
        private readonly List<string> _lines;

        /// <summary>
        /// Initializes a new SMTP response
        /// </summary>
        public SmtpResponse(int code, string message)
        {
            if (code is < 100 or > 599)
            {
                throw new ArgumentOutOfRangeException(nameof(code), "SMTP response code must be between 100 and 599");
            }

            Code = code;
            _lines = new List<string>();

            if (!string.IsNullOrEmpty(message))
            {
                string[] lines = message.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                _lines.AddRange(lines);
            }

            if (_lines.Count == 0)
            {
                _lines.Add(GetDefaultMessage(code));
            }
        }

        /// <summary>
        /// Initializes a new SMTP response with multiple lines
        /// </summary>
        public SmtpResponse(int code, params string[] lines)
        {
            if (code is < 100 or > 599)
            {
                throw new ArgumentOutOfRangeException(nameof(code), "SMTP response code must be between 100 and 599");
            }

            Code = code;
            _lines = new List<string>();

            if (lines != null && lines.Length > 0)
            {
                foreach (string line in lines)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        string[] splitLines = line.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        _lines.AddRange(splitLines);
                    }
                }
            }

            if (_lines.Count == 0)
            {
                _lines.Add(GetDefaultMessage(code));
            }
        }

        /// <summary>
        /// Gets the response code
        /// </summary>
        public int Code { get; }

        /// <summary>
        /// Gets the response message lines
        /// </summary>
        public IReadOnlyList<string> Lines => _lines;

        /// <summary>
        /// Gets the first line of the message
        /// </summary>
        public string Message => _lines.FirstOrDefault() ?? string.Empty;

        /// <summary>
        /// Gets whether this is a positive response (2xx)
        /// </summary>
        public bool IsPositive => Code is >= 200 and < 300;

        /// <summary>
        /// Gets whether this is a positive intermediate response (3xx)
        /// </summary>
        public bool IsPositiveIntermediate => Code is >= 300 and < 400;

        /// <summary>
        /// Gets whether this is a transient negative response (4xx)
        /// </summary>
        public bool IsTransientNegative => Code is >= 400 and < 500;

        /// <summary>
        /// Gets whether this is a permanent negative response (5xx)
        /// </summary>
        public bool IsPermanentNegative => Code is >= 500 and < 600;

        /// <summary>
        /// Gets whether this is a successful response
        /// </summary>
        public bool IsSuccess => IsPositive || IsPositiveIntermediate;

        /// <summary>
        /// Gets whether this is an error response
        /// </summary>
        public bool IsError => IsTransientNegative || IsPermanentNegative;

        /// <summary>
        /// Converts the response to SMTP protocol format
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new();

            if (_lines.Count == 0)
            {
                sb.Append(Code).Append(' ').AppendLine(GetDefaultMessage(Code));
            }
            else if (_lines.Count == 1)
            {
                sb.Append(Code).Append(' ').AppendLine(_lines[0]);
            }
            else
            {
                for (int i = 0; i < _lines.Count; i++)
                {
                    sb.Append(Code);
                    if (i < _lines.Count - 1)
                    {
                        sb.Append('-');
                    }
                    else
                    {
                        sb.Append(' ');
                    }

                    sb.AppendLine(_lines[i]);
                }
            }

            return sb.ToString();
        }

        private static string GetDefaultMessage(int code)
        {
            return code switch
            {
                220 => "Service ready",
                221 => "Service closing transmission channel",
                250 => "OK",
                251 => "User not local; will forward",
                252 => "Cannot VRFY user, but will accept message and attempt delivery",
                354 => "Start mail input; end with <CRLF>.<CRLF>",
                421 => "Service not available, closing transmission channel",
                450 => "Requested mail action not taken: mailbox unavailable",
                451 => "Requested action aborted: local error in processing",
                452 => "Requested action not taken: insufficient system storage",
                500 => "Syntax error, command unrecognized",
                501 => "Syntax error in parameters or arguments",
                502 => "Command not implemented",
                503 => "Bad sequence of commands",
                504 => "Command parameter not implemented",
                550 => "Requested action not taken: mailbox unavailable",
                551 => "User not local; please try forward path",
                552 => "Requested mail action aborted: exceeded storage allocation",
                553 => "Requested action not taken: mailbox name not allowed",
                554 => "Transaction failed",
                _ => "Unknown response"
            };
        }

        #region Common Responses

        /// <summary>220 Service ready</summary>
        public static readonly SmtpResponse ServiceReady = new(220, "Service ready");

        /// <summary>221 Service closing transmission channel</summary>
        public static readonly SmtpResponse ServiceClosing = new(221, "Service closing transmission channel");

        /// <summary>250 OK</summary>
        public static readonly SmtpResponse Ok = new(250, "OK");

        /// <summary>250 Message accepted</summary>
        public static readonly SmtpResponse MessageAccepted = new(250, "Message accepted");

        /// <summary>251 User not local; will forward</summary>
        public static readonly SmtpResponse UserNotLocalWillForward = new(251, "User not local; will forward");

        /// <summary>252 Cannot VRFY user</summary>
        public static readonly SmtpResponse CannotVerifyUser = new(252, "Cannot VRFY user, but will accept message and attempt delivery");

        /// <summary>354 Start mail input</summary>
        public static readonly SmtpResponse StartMailInput = new(354, "Start mail input; end with <CRLF>.<CRLF>");

        /// <summary>421 Service not available</summary>
        public static readonly SmtpResponse ServiceNotAvailable = new(421, "Service not available, closing transmission channel");

        /// <summary>450 Mailbox unavailable</summary>
        public static readonly SmtpResponse MailboxUnavailable = new(450, "Requested mail action not taken: mailbox unavailable");

        /// <summary>451 Local error</summary>
        public static readonly SmtpResponse LocalError = new(451, "Requested action aborted: local error in processing");

        /// <summary>452 Insufficient storage</summary>
        public static readonly SmtpResponse InsufficientStorage = new(452, "Requested action not taken: insufficient system storage");

        /// <summary>500 Syntax error</summary>
        public static readonly SmtpResponse SyntaxError = new(500, "Syntax error, command unrecognized");

        /// <summary>501 Syntax error in parameters</summary>
        public static readonly SmtpResponse SyntaxErrorInParameters = new(501, "Syntax error in parameters or arguments");

        /// <summary>502 Command not implemented</summary>
        public static readonly SmtpResponse CommandNotImplemented = new(502, "Command not implemented");

        /// <summary>503 Bad sequence</summary>
        public static readonly SmtpResponse BadSequence = new(503, "Bad sequence of commands");

        /// <summary>504 Parameter not implemented</summary>
        public static readonly SmtpResponse ParameterNotImplemented = new(504, "Command parameter not implemented");

        /// <summary>530 Authentication required</summary>
        public static readonly SmtpResponse AuthenticationRequired = new(530, "Authentication required");

        /// <summary>535 Authentication failed</summary>
        public static readonly SmtpResponse AuthenticationFailed = new(535, "Authentication failed");

        /// <summary>550 Mailbox not found</summary>
        public static readonly SmtpResponse MailboxNotFound = new(550, "Requested action not taken: mailbox unavailable");

        /// <summary>551 User not local</summary>
        public static readonly SmtpResponse UserNotLocal = new(551, "User not local; please try forward path");

        /// <summary>552 Storage exceeded</summary>
        public static readonly SmtpResponse StorageExceeded = new(552, "Requested mail action aborted: exceeded storage allocation");

        /// <summary>553 Mailbox name not allowed</summary>
        public static readonly SmtpResponse MailboxNameNotAllowed = new(553, "Requested action not taken: mailbox name not allowed");

        /// <summary>554 Transaction failed</summary>
        public static readonly SmtpResponse TransactionFailed = new(554, "Transaction failed");

        #region TLS and Authentication Responses

        /// <summary>220 Ready to start TLS</summary>
        public static readonly SmtpResponse ReadyToStartTls = new(220, "Ready to start TLS");

        /// <summary>235 Authentication successful</summary>
        public static readonly SmtpResponse AuthenticationSuccessful = new(235, "Authentication successful");

        /// <summary>452 Too many recipients</summary>
        public static readonly SmtpResponse TooManyRecipients = new(452, "Too many recipients");

        /// <summary>504 Authentication mechanism not supported</summary>
        public static readonly SmtpResponse AuthMechanismNotSupported = new(504, "Authentication mechanism not supported");

        /// <summary>538 Encryption required for authentication</summary>
        public static readonly SmtpResponse EncryptionRequiredForAuth = new(538, "Encryption required for authentication");

        /// <summary>550 Sender rejected</summary>
        public static readonly SmtpResponse SenderRejected = new(550, "Sender rejected");

        /// <summary>550 Recipient rejected</summary>
        public static readonly SmtpResponse RecipientRejected = new(550, "Recipient rejected");

        #endregion

        #endregion
    }
}