using System;
using Amazon.Lambda;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

using Amazon.Lambda.Serialization.SystemTextJson;

using Amazon.ApiGatewayManagementApi;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.Lambda.SQSEvents;

using Amazon;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Redis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HotChocolate.Language;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
namespace GraphQLDataServer
{
    /// <summary>
    /// This class extends from APIGatewayProxyFunction which contains the method FunctionHandlerAsync which is the 
    /// actual Lambda function entry point. The Lambda handler field should be set to
    /// 
    /// MyHttpGatewayApi::MyHttpGatewayApi.LambdaEntryPoint::FunctionHandlerAsync
    /// </summary>
    public class LambdaEntryPoint : Amazon.Lambda.AspNetCoreServer.APIGatewayHttpApiV2ProxyFunction
    {
        IAmazonSQS SQSClient { get; set; }
        IMessageSerializer msgInitialiser { get; set; }
        Func<string, IAmazonApiGatewayManagementApi> ApiGatewayManagementApiClientFactory { get; set; }
        

        protected override void Init(IWebHostBuilder builder)
        {
            builder
                .UseStartup<Startup>();
        }

        /// <summary>
        /// Use this override to customize the services registered with the IHostBuilder. 
        /// 
        /// It is recommended not to call ConfigureWebHostDefaults to configure the IWebHostBuilder inside this method.
        /// Instead customize the IWebHostBuilder in the Init(IWebHostBuilder) overload.
        /// </summary>
        /// <param name="builder"></param>
        protected override void Init(IHostBuilder builder)
        {
            SQSClient = new AmazonSQSClient();
            var serviceProvider = new ServiceCollection()

                .AddSingleton<IMessageSerializer, JsonMessageSerializer>()

                .BuildServiceProvider();

             msgInitialiser = serviceProvider.GetService<IMessageSerializer>();

        }

        public async ValueTask HandleGraphQLMessage(SQSEvent sqsEvent, ILambdaContext context)
        {
            
            try
            {
                var awsCredentials = new BasicAWSCredentials("AKIAUCTAWCDCVW4OY5R5", "Nh6KeyODYrPhYCikuq8obbfAH6Rjj/JbPX5TCaNb");
                var region = RegionEndpoint.GetBySystemName("us-east-1");

               
                 CreateRedis();
                
                var val = GetMessageType();
                var serializedMessage = msgInitialiser.Serialize(val);
                serializedMessage = serializedMessage.Replace("GraphQLDataServer", "HotChocolateService");
                MessageType mt = null;
                string resp = null;
                foreach (var record in sqsEvent.Records)
                {
                            Console.WriteLine($"In Graphql server {record.Body}");
                       
                        using (var client = new AmazonLambdaClient(awsCredentials, region))
                        {
                        var request_ = new Amazon.Lambda.Model.InvokeRequest
                        {
                            FunctionName = "HotchocolateService-GraphQLSentMessageFunction-1G8BUV9IW11FK",
                            InvocationType = InvocationType.RequestResponse,
                            LogType = LogType.Tail,
                            Payload = serializedMessage
                        };

                            var result = Task.Run(() => _ = client.InvokeAsync(request_));
                           var response = result.GetAwaiter().GetResult();
                            var bytes = new byte[response.Payload.Length];
                            response.Payload.Seek(0, SeekOrigin.Begin);
                            await response.Payload.ReadAsync(bytes, 0, bytes.Length);
                            resp = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                            Console.WriteLine($"Response: {resp}");
                           mt = msgInitialiser.Deserialize<MessageType>(resp);

                    }

                }

                using (var client = new AmazonLambdaClient(awsCredentials, region))
                {

                    var o = msgInitialiser.Serialize<MessageType>(mt);
                    o = o.Replace("GraphQLDataServer", "AWSServerless2");
                    var request = new Amazon.Lambda.Model.InvokeRequest
                    {
                        FunctionName = "webSockStack-SendMessageFunction-MRTK1PFLIITB",
 //"webSockStack-SendGraphQLResult-1WTWRV7H7ZXI1",
                        InvocationType = InvocationType.RequestResponse,
                        LogType = LogType.Tail,
                        Payload = o
                    };
                    var result = Task.Run(() => _ = client.InvokeAsync(request));
                    var response = result.GetAwaiter().GetResult();
                    Console.WriteLine($"Big Resp {response}");
                }
                
            }
            catch (Exception e) { Console.WriteLine($"Error: {e.Message}"); };
        }

        private  MessageType GetMessageType()
        {
            MessageType msgType = null;
        
            using (var manager = new RedisManagerPool("messagetyperedis-001.03brtg.0001.use1.cache.amazonaws.com:6379"))
            {
                using (var redis = manager.GetClient())
                {
                    var x = redis.As<MessageType>();
                    var m = x.GetAll();
                    msgType = m[0];
                    Console.WriteLine($"MessageType {m[0].Subscription}");
                }
            }
            return msgType;
        }

        private async ValueTask CreateRedis()
        {

            var from = new MessageFrom();
            from.DisplayName = "test";
            from.Id = "1";
            var msg = new Message();
            msg.Content = "Content Test";
            msg.MessageFrom = from;
            msg.SentAt = DateTime.Now.ToUniversalTime();
             
             var serializedMessage = msgInitialiser.Serialize(msg);                
             serializedMessage = serializedMessage.Replace("GraphQLDataServer", "HotChocolateService");
            Console.WriteLine($"Serilaised message {serializedMessage}");
             var resp = Encoding.UTF8.GetBytes(serializedMessage.ToCharArray());
            

            using (var manager = new RedisManagerPool("redisserver-001.03brtg.0001.use1.cache.amazonaws.com:6379"))
            {
                using (var redis = manager.GetClient())
                {
                    // redis.Set("MessageAdded", "tetst");
                    redis.Store<Message>(msg);
                  //  var redisUsers = redis.As<Message>();
                    LambdaLogger.Log("redis client created");
                }
            }
            //    var client = redisManager.RedisResolver.CreateRedisClient(redisEndpoint, true);

            /*using (var client = redisManager.GetClient())
            {
                client.Set("foo", "bar");
                Console.WriteLine("foo={0}", client.Get<string>("foo"));
            }*/

            /*  using (var redis = new RedisClient("redisserver-001.03brtg.0001.use1.cache.amazonaws.com", 6379))
               {
                   var redisUsers = redis.As<Message>();
                   LambdaLogger.Log("redis client created");

                   var user = msg;
                   redisUsers.Store(user);
                   LambdaLogger.Log("user added");

                   var allUsers = redisUsers.GetAll();
                   LambdaLogger.Log("Retrieved users");

               }*/

        }

    }
}
