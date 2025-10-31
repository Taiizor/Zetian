using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zetian.Configuration;
using Zetian.WebUI.Controllers;

namespace Zetian.WebUI.Services
{
    /// <summary>
    /// Service for configuration management
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Gets the current configuration
        /// </summary>
        Task<ConfigurationDto> GetConfigurationAsync();

        /// <summary>
        /// Updates the configuration
        /// </summary>
        Task<ConfigurationUpdateResult> UpdateConfigurationAsync(ConfigurationUpdateRequest request);

        /// <summary>
        /// Validates configuration
        /// </summary>
        Task<ValidationResult> ValidateConfigurationAsync(ConfigurationUpdateRequest request);

        /// <summary>
        /// Exports configuration
        /// </summary>
        Task<byte[]> ExportConfigurationAsync(string format = "json");

        /// <summary>
        /// Imports configuration
        /// </summary>
        Task<ConfigurationUpdateResult> ImportConfigurationAsync(byte[] data, string format = "json");

        /// <summary>
        /// Resets configuration to defaults
        /// </summary>
        Task<ConfigurationUpdateResult> ResetToDefaultsAsync();

        /// <summary>
        /// Gets configuration history
        /// </summary>
        Task<IEnumerable<ConfigurationHistoryItem>> GetHistoryAsync();
    }

    /// <summary>
    /// Configuration DTO
    /// </summary>
    public class ConfigurationDto
    {
        public int Port { get; set; }
        public string IpAddress { get; set; } = "";
        public string ServerName { get; set; } = "";
        public long MaxMessageSize { get; set; }
        public int MaxRecipients { get; set; }
        public int MaxConnections { get; set; }
        public int MaxConnectionsPerIp { get; set; }
        public TimeSpan ConnectionTimeout { get; set; }
        public TimeSpan CommandTimeout { get; set; }
        public TimeSpan DataTimeout { get; set; }
        public int MaxRetryCount { get; set; }
        public bool EnablePipelining { get; set; }
        public bool Enable8BitMime { get; set; }
        public bool EnableBinaryMime { get; set; }
        public bool EnableChunking { get; set; }
        public bool EnableSmtpUtf8 { get; set; }
        public bool EnableSizeExtension { get; set; }
        public bool RequireAuthentication { get; set; }
        public bool RequireSecureConnection { get; set; }
        public bool AllowPlainTextAuthentication { get; set; }
        public List<string> AuthenticationMechanisms { get; set; } = [];
        public string? Banner { get; set; }
        public string? Greeting { get; set; }
        public bool EnableVerboseLogging { get; set; }
        public int ReadBufferSize { get; set; }
        public int WriteBufferSize { get; set; }
        public bool UseNagleAlgorithm { get; set; }

        public static ConfigurationDto FromConfiguration(SmtpServerConfiguration config)
        {
            return new ConfigurationDto
            {
                Port = config.Port,
                IpAddress = config.IpAddress.ToString(),
                ServerName = config.ServerName,
                MaxMessageSize = config.MaxMessageSize,
                MaxRecipients = config.MaxRecipients,
                MaxConnections = config.MaxConnections,
                MaxConnectionsPerIp = config.MaxConnectionsPerIp,
                ConnectionTimeout = config.ConnectionTimeout,
                CommandTimeout = config.CommandTimeout,
                DataTimeout = config.DataTimeout,
                MaxRetryCount = config.MaxRetryCount,
                EnablePipelining = config.EnablePipelining,
                Enable8BitMime = config.Enable8BitMime,
                EnableBinaryMime = config.EnableBinaryMime,
                EnableChunking = config.EnableChunking,
                EnableSmtpUtf8 = config.EnableSmtpUtf8,
                EnableSizeExtension = config.EnableSizeExtension,
                RequireAuthentication = config.RequireAuthentication,
                RequireSecureConnection = config.RequireSecureConnection,
                AllowPlainTextAuthentication = config.AllowPlainTextAuthentication,
                AuthenticationMechanisms = [.. config.AuthenticationMechanisms],
                Banner = config.Banner,
                Greeting = config.Greeting,
                EnableVerboseLogging = config.EnableVerboseLogging,
                ReadBufferSize = config.ReadBufferSize,
                WriteBufferSize = config.WriteBufferSize,
                UseNagleAlgorithm = config.UseNagleAlgorithm
            };
        }
    }

    /// <summary>
    /// Configuration update result
    /// </summary>
    public class ConfigurationUpdateResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public bool RequiresRestart { get; set; }
        public List<string> ChangedProperties { get; set; } = [];
    }

    /// <summary>
    /// Validation result
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = [];
    }

    /// <summary>
    /// Validation error
    /// </summary>
    public class ValidationError
    {
        public string Property { get; set; } = "";
        public string Message { get; set; } = "";
    }

    /// <summary>
    /// Configuration history item
    /// </summary>
    public class ConfigurationHistoryItem
    {
        public DateTime Timestamp { get; set; }
        public string User { get; set; } = "";
        public string Action { get; set; } = "";
        public Dictionary<string, object> Changes { get; set; } = [];
    }
}