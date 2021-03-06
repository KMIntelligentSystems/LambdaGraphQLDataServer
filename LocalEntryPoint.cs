﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GraphQLDataServer
{
    /// <summary>
    /// The Main function can be used to run the ASP.NET Core application locally using the Kestrel webserver.
    /// </summary>
    public class LocalEntryPoint
    {
        static IConfiguration Configuration;
        public static void Main(string[] args)
        {
            Configuration = new ConfigurationBuilder()
            //  .AddSystemsManager("/lambda")
              .Build();

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
           //       services.AddSingleton<Worker>();   
              //  services.AddLogging(l => l.AddConfiguration(Configuration));
              //  services.AddHostedService<Worker>();
             //   services.AddScoped<IGraphQLServiceWorker, GraphQLServiceWorker>();
            });
    }
}