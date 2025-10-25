using Microsoft.Extensions.Logging;
using System;
using Zetian.Server;
using Zetian.Storage.Extensions;

namespace Zetian.Storage.AzureBlob.Extensions
{
    /// <summary>
    /// Extension methods for configuring Azure Blob Storage
    /// </summary>
    public static class AzureBlobStorageExtensions
    {
        /// <summary>
        /// Configures Azure Blob Storage as the message storage provider
        /// </summary>
        public static SmtpServerBuilder WithAzureBlobStorage(
            this SmtpServerBuilder builder,
            string connectionString,
            Action<AzureBlobStorageConfiguration>? configure = null)
        {
            AzureBlobStorageConfiguration configuration = new()
            {
                ConnectionString = connectionString
            };

            configure?.Invoke(configuration);
            configuration.Validate();

            ILogger<AzureBlobMessageStore>? logger = builder.GetLogger<AzureBlobMessageStore>();
            AzureBlobMessageStore store = new(configuration, logger);

            return builder.MessageStore(store);
        }

        /// <summary>
        /// Configures Azure Blob Storage with Azure AD authentication
        /// </summary>
        public static SmtpServerBuilder WithAzureBlobStorageAD(
            this SmtpServerBuilder builder,
            string storageAccountName,
            Action<AzureBlobStorageConfiguration>? configure = null)
        {
            AzureBlobStorageConfiguration configuration = new()
            {
                UseAzureAdAuthentication = true,
                StorageAccountName = storageAccountName
            };

            configure?.Invoke(configuration);
            configuration.Validate();

            ILogger<AzureBlobMessageStore>? logger = builder.GetLogger<AzureBlobMessageStore>();
            AzureBlobMessageStore store = new(configuration, logger);

            return builder.MessageStore(store);
        }
    }
}