using System;
using ServiceStack.Redis;

namespace GraphQLDataServer
{
    public class MessageHandler<TSource>
              where TSource : class
    {

        public void StoreMessage(TSource msg)
        {
            using (var manager = new RedisManagerPool("redisserver-001.03brtg.0001.use1.cache.amazonaws.com:6379"))
            {
                using (var redis = manager.GetClient())
                {
                    redis.Store<TSource>(msg);
                }
            }
        }
    }
}
