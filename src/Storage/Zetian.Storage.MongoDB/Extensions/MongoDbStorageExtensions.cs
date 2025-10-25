using Microsoft.Extensions.Logging;
using System;
using Zetian.Server;
using Zetian.Storage.Extensions;
using Zetian.Storage.MongoDB.Configuration;
using Zetian.Storage.MongoDB.Storage;

namespace Zetian.Storage.MongoDB.Extensions
{
    /// <summary>
    /// Extension methods for configuring MongoDB storage
    /// </summary>
    public static class MongoDbStorageExtensions
    {
        /// <summary>
        /// Configures MongoDB as the message storage provider
        /// </summary>
        public static SmtpServerBuilder WithMongoDbStorage(
            this SmtpServerBuilder builder,
            string connectionString,
            string databaseName,
            Action<MongoDbStorageConfiguration>? configure = null)
        {
            MongoDbStorageConfiguration configuration = new()
            {
                ConnectionString = connectionString,
                DatabaseName = databaseName
            };

            configure?.Invoke(configuration);
            configuration.Validate();

            ILogger<MongoDbMessageStore>? logger = builder.GetLogger<MongoDbMessageStore>();
            MongoDbMessageStore store = new(configuration, logger);

            return builder.MessageStore(store);
        }
    }
}