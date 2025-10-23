using System;
using System.Collections.Generic;

namespace Zetian.Protocol
{
    /// <summary>
    /// Represents an SMTP command
    /// </summary>
    public class SmtpCommand
    {
        /// <summary>
        /// Common SMTP commands
        /// </summary>
        public static class Commands
        {
            public const string HELO = "HELO";
            public const string EHLO = "EHLO";
            public const string MAIL = "MAIL";
            public const string RCPT = "RCPT";
            public const string DATA = "DATA";
            public const string RSET = "RSET";
            public const string QUIT = "QUIT";
            public const string NOOP = "NOOP";
            public const string VRFY = "VRFY";
            public const string EXPN = "EXPN";
            public const string HELP = "HELP";
            public const string AUTH = "AUTH";
            public const string STARTTLS = "STARTTLS";
            public const string BDAT = "BDAT";
            public const string TURN = "TURN";
            public const string ATRN = "ATRN";
            public const string SIZE = "SIZE";
            public const string ETRN = "ETRN";
        }

        /// <summary>
        /// Initializes a new SMTP command
        /// </summary>
        public SmtpCommand(string verb, string? argument = null)
        {
            if (string.IsNullOrWhiteSpace(verb))
            {
                throw new ArgumentException("Command verb cannot be empty", nameof(verb));
            }

            Verb = verb.ToUpperInvariant();
            Argument = argument?.Trim();
            Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            ParseParameters();
        }

        /// <summary>
        /// Gets the command verb
        /// </summary>
        public string Verb { get; }

        /// <summary>
        /// Gets the command argument
        /// </summary>
        public string? Argument { get; }

        /// <summary>
        /// Gets the command parameters
        /// </summary>
        public IDictionary<string, string> Parameters { get; }

        /// <summary>
        /// Parses a command line
        /// </summary>
        public static SmtpCommand Parse(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
            {
                throw new ArgumentException("Command line cannot be empty", nameof(commandLine));
            }

            commandLine = commandLine.Trim();
            int spaceIndex = commandLine.IndexOf(' ');

            if (spaceIndex == -1)
            {
                return new SmtpCommand(commandLine);
            }

            string verb = commandLine[..spaceIndex];
            string argument = commandLine[(spaceIndex + 1)..].Trim();

            return new SmtpCommand(verb, argument);
        }

        /// <summary>
        /// Tries to parse a command line
        /// </summary>
        public static bool TryParse(string commandLine, out SmtpCommand? command)
        {
            command = null;

            try
            {
                command = Parse(commandLine);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ParseParameters()
        {
            if (string.IsNullOrWhiteSpace(Argument))
            {
                return;
            }

            // Parse MAIL FROM and RCPT TO parameters
            if (Verb is Commands.MAIL or Commands.RCPT)
            {
                string[] parts = Argument.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (string part in parts)
                {
                    if (part.Contains('='))
                    {
                        string[] kvp = part.Split('=', 2);
                        if (kvp.Length == 2)
                        {
                            Parameters[kvp[0]] = kvp[1];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets whether this is a HELO command
        /// </summary>
        public bool IsHelo => Verb == Commands.HELO;

        /// <summary>
        /// Gets whether this is an EHLO command
        /// </summary>
        public bool IsEhlo => Verb == Commands.EHLO;

        /// <summary>
        /// Gets whether this is a MAIL command
        /// </summary>
        public bool IsMail => Verb == Commands.MAIL;

        /// <summary>
        /// Gets whether this is a RCPT command
        /// </summary>
        public bool IsRcpt => Verb == Commands.RCPT;

        /// <summary>
        /// Gets whether this is a DATA command
        /// </summary>
        public bool IsData => Verb == Commands.DATA;

        /// <summary>
        /// Gets whether this is a QUIT command
        /// </summary>
        public bool IsQuit => Verb == Commands.QUIT;

        /// <summary>
        /// Gets whether this is a RSET command
        /// </summary>
        public bool IsRset => Verb == Commands.RSET;

        /// <summary>
        /// Gets whether this is a NOOP command
        /// </summary>
        public bool IsNoop => Verb == Commands.NOOP;

        /// <summary>
        /// Gets whether this is an AUTH command
        /// </summary>
        public bool IsAuth => Verb == Commands.AUTH;

        /// <summary>
        /// Gets whether this is a STARTTLS command
        /// </summary>
        public bool IsStartTls => Verb == Commands.STARTTLS;

        /// <summary>
        /// Returns the command as a string
        /// </summary>
        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Argument)
                ? Verb
                : $"{Verb} {Argument}";
        }
    }
}