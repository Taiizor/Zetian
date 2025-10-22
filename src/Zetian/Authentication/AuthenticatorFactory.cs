using System;
using System.Collections.Generic;
using Zetian.Abstractions;

namespace Zetian.Authentication
{
    /// <summary>
    /// Factory for creating authenticators
    /// </summary>
    public static class AuthenticatorFactory
    {
        private static readonly Dictionary<string, Func<IAuthenticator>> _authenticators = new(StringComparer.OrdinalIgnoreCase);
        private static AuthenticationHandler? _defaultHandler;

        static AuthenticatorFactory()
        {
            // Register default authenticators
            Register("PLAIN", () => new PlainAuthenticator(_defaultHandler));
            Register("LOGIN", () => new LoginAuthenticator(_defaultHandler));
        }

        /// <summary>
        /// Sets the default authentication handler
        /// </summary>
        public static void SetDefaultHandler(AuthenticationHandler handler)
        {
            _defaultHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <summary>
        /// Registers a custom authenticator
        /// </summary>
        public static void Register(string mechanism, Func<IAuthenticator> factory)
        {
            if (string.IsNullOrWhiteSpace(mechanism))
            {
                throw new ArgumentException("Mechanism cannot be empty", nameof(mechanism));
            }

            _authenticators[mechanism] = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Creates an authenticator for the specified mechanism
        /// </summary>
        public static IAuthenticator? Create(string mechanism)
        {
            if (_authenticators.TryGetValue(mechanism, out Func<IAuthenticator>? factory))
            {
                return factory();
            }

            return null;
        }

        /// <summary>
        /// Gets the registered mechanism names
        /// </summary>
        public static IEnumerable<string> GetMechanisms()
        {
            return _authenticators.Keys;
        }

        /// <summary>
        /// Clears all registered authenticators
        /// </summary>
        public static void Clear()
        {
            _authenticators.Clear();
        }

        /// <summary>
        /// Resets to default authenticators
        /// </summary>
        public static void Reset()
        {
            Clear();
            Register("PLAIN", () => new PlainAuthenticator(_defaultHandler));
            Register("LOGIN", () => new LoginAuthenticator(_defaultHandler));
        }
    }
}