using Microsoft.Extensions.Logging;
using System;
using Zetian.Server;
using Zetian.Storage.Extensions;
using Zetian.Storage.Redis.Configuration;
using Zetian.Storage.Redis.Storage;

namespace Zetian.Storage.Redis.Extensions
{
    /// <summary>
    /// Extension methods for configuring Redis storage
    /// </summary>
    public static class RedisStorageExtensions
    {
        /// <summary>
        /// Configures Redis as the message storage/cache provider
        /// </summary>
        public static SmtpServerBuilder WithRedisStorage(
            this SmtpServerBuilder builder,
            string connectionString,
            Action<RedisStorageConfiguration>? configure = null)
        {
            RedisStorageConfiguration configuration = new()
            {
                ConnectionString = connectionString
            };

            configure?.Invoke(configuration);
            configuration.Validate();

            ILogger<RedisMessageStore>? logger = builder.GetLogger<RedisMessageStore>();
            RedisMessageStore store = new(configuration, logger);

            return builder.MessageStore(store);
        }
    }
}