using ServiceStack;
using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQLDataServer
{
    public class ElasticCacheRedisResolver : RedisResolver
    {
        public override RedisClient CreateRedisClient(RedisEndpoint config, bool master)
        {
            if (master)
            {
                //ElastiCache Redis will failover & retain same DNS for master
                var firstAttempt = DateTime.UtcNow;
                Exception firstEx = null;
                var retryTimeSpan = TimeSpan.FromMilliseconds(config.RetryTimeout);
                var i = 0;
                while (DateTime.UtcNow - firstAttempt < retryTimeSpan)
                {
                    try
                    {
                        var client = base.CreateRedisClient(config, master: true);
                        return client;
                    }
                    catch (Exception ex)
                    {
                        firstEx ??= ex;
                        ExecUtils.SleepBackOffMultiplier(++i);
                    }
                }
                throw new TimeoutException(
                  $"Could not resolve master within {config.RetryTimeout}ms RetryTimeout", firstEx);
            }
            return base.CreateRedisClient(config, master: false);
        }
    }
}
