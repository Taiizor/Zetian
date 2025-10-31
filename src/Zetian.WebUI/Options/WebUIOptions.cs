using System;
using System.Security.Cryptography.X509Certificates;

namespace Zetian.WebUI.Options
{
    /// <summary>
    /// Configuration options for the WebUI
    /// </summary>
    public class WebUIOptions
    {
        /// <summary>
        /// Gets or sets the port to listen on
        /// </summary>
        public int Port { get; set; } = 8080;

        /// <summary>
        /// Gets or sets the hostname to bind to
        /// </summary>
        public string? HostName { get; set; }

        /// <summary>
        /// Gets or sets the base path for the WebUI
        /// </summary>
        public string BasePath { get; set; } = "/";

        /// <summary>
        /// Gets or sets whether to use HTTPS
        /// </summary>
        public bool UseHttps { get; set; } = false;

        /// <summary>
        /// Gets or sets the SSL certificate for HTTPS
        /// </summary>
        public X509Certificate2? Certificate { get; set; }

        /// <summary>
        /// Gets or sets whether authentication is required
        /// </summary>
        public bool RequireAuthentication { get; set; } = true;

        /// <summary>
        /// Gets or sets the admin username
        /// </summary>
        public string AdminUsername { get; set; } = "admin";

        /// <summary>
        /// Gets or sets the admin password
        /// </summary>
        public string AdminPassword { get; set; } = "admin";

        /// <summary>
        /// Gets or sets whether API key authentication is enabled
        /// </summary>
        public bool EnableApiKey { get; set; } = false;

        /// <summary>
        /// Gets or sets the API key
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the session timeout
        /// </summary>
        public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Gets or sets the JWT secret key
        /// </summary>
        public string JwtSecretKey { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the JWT issuer
        /// </summary>
        public string JwtIssuer { get; set; } = "Zetian.WebUI";

        /// <summary>
        /// Gets or sets the JWT audience
        /// </summary>
        public string JwtAudience { get; set; } = "Zetian.WebUI.Client";

        /// <summary>
        /// Gets or sets whether Swagger is enabled
        /// </summary>
        public bool EnableSwagger { get; set; } = true;

        /// <summary>
        /// Gets or sets whether SignalR is enabled
        /// </summary>
        public bool EnableSignalR { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the metrics endpoint is enabled
        /// </summary>
        public bool EnableMetricsEndpoint { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the log viewer is enabled
        /// </summary>
        public bool EnableLogViewer { get; set; } = true;

        /// <summary>
        /// Gets or sets whether CORS is enabled
        /// </summary>
        public bool EnableCors { get; set; } = false;

        /// <summary>
        /// Gets or sets the allowed CORS origins
        /// </summary>
        public string[]? CorsOrigins { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of log entries to keep
        /// </summary>
        public int MaxLogEntries { get; set; } = 5000;

        /// <summary>
        /// Gets or sets the maximum number of session history entries
        /// </summary>
        public int MaxSessionHistory { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the metrics retention period in days
        /// </summary>
        public int MetricsRetentionDays { get; set; } = 7;

        /// <summary>
        /// Gets or sets the refresh interval for real-time updates (in seconds)
        /// </summary>
        public int RefreshInterval { get; set; } = 5;

        /// <summary>
        /// Gets or sets whether to enable rate limiting
        /// </summary>
        public bool EnableRateLimiting { get; set; } = true;

        /// <summary>
        /// Gets or sets the rate limit per minute
        /// </summary>
        public int RateLimitPerMinute { get; set; } = 60;

        /// <summary>
        /// Gets or sets the UI theme
        /// </summary>
        public string Theme { get; set; } = "light";

        /// <summary>
        /// Gets or sets the UI title
        /// </summary>
        public string Title { get; set; } = "Zetian SMTP Server";

        /// <summary>
        /// Gets or sets the logo URL
        /// </summary>
        public string? LogoUrl { get; set; }

        /// <summary>
        /// Gets or sets whether to show the footer
        /// </summary>
        public bool ShowFooter { get; set; } = true;

        /// <summary>
        /// Validates the options
        /// </summary>
        public void Validate()
        {
            if (Port is < 1 or > 65535)
            {
                throw new ArgumentException("Port must be between 1 and 65535");
            }

            if (UseHttps && Certificate == null)
            {
                throw new ArgumentException("Certificate is required when UseHttps is true");
            }

            if (RequireAuthentication)
            {
                if (string.IsNullOrWhiteSpace(AdminUsername))
                {
                    throw new ArgumentException("AdminUsername cannot be empty when authentication is required");
                }

                if (string.IsNullOrWhiteSpace(AdminPassword))
                {
                    throw new ArgumentException("AdminPassword cannot be empty when authentication is required");
                }
            }

            if (EnableApiKey && string.IsNullOrWhiteSpace(ApiKey))
            {
                throw new ArgumentException("ApiKey cannot be empty when API key authentication is enabled");
            }

            if (SessionTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("SessionTimeout must be greater than zero");
            }

            if (MaxLogEntries < 0)
            {
                throw new ArgumentException("MaxLogEntries cannot be negative");
            }

            if (MaxSessionHistory < 0)
            {
                throw new ArgumentException("MaxSessionHistory cannot be negative");
            }

            if (MetricsRetentionDays < 0)
            {
                throw new ArgumentException("MetricsRetentionDays cannot be negative");
            }
        }
    }
}