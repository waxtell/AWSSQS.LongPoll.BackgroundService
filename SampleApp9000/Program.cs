using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWSSQS.LongPoll.BackgroundService;
using AWSSQS.LongPoll.BackgroundService.Models.External;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SampleApp9000.Extensions;

// ReSharper disable StringLiteralTypo

namespace SampleApp9000
{
    public class Program
    {
        // AppVeyor currently does not provide an environment that supports c# 7.1
        // This makes main synchronous and me sad.
        //
        //public static async Task Main(string[] args)
        //{
        //    await
        //        CreateHostBuilder(args)
        //            .RunConsoleAsync();
        //}

        public static void Main(string[] args)
        {
            CreateHostBuilder(args)
                .Build()
                .Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            new HostBuilder()
                .ConfigureAppConfiguration
                (
                    (hostContext, config) =>
                    {
                        config.SetBasePath(Directory.GetCurrentDirectory());
                        config.AddEnvironmentVariables();
                        config.AddJsonFile("appsettings.json", optional: true);
                        config.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json",
                            optional: true);
                        config.AddCommandLine(args);
                    }
                )
                .ConfigureServices
                (
                    (context, collection) =>
                    {
                        collection
                            .AddSingleton
                            (
                                provider =>
                                {
                                    return new Events
                                    {
                                        OnMessageReceived = async (message, cancellationToken) =>
                                        {
                                            Console.WriteLine($"Received: {message.Body}");
                                            Thread.Sleep(10000);
                                            await Task.CompletedTask;
                                        },
                                        OnException = async (exception, cancellationToken) =>
                                        {
                                            Console.WriteLine($"Exception: {exception.Message}");

                                            // Return false to quit, true to continue trying...
                                            Thread.Sleep(10000);
                                            return await Task.FromResult(true);
                                        }
                                    };
                                }
                            );

                        collection
                            .AddSingleton<Func<ReceiveMessageRequest>>
                            (
                                provider =>
                                {
                                    return 
                                        () => provider
                                                .GetService<IConfiguration>()
                                                .GetSection("ReceiveMessageRequest")
                                                .Get<ReceiveMessageRequest>();
                                }
                            );

                        collection
                            .AddSingleton
                            (
                                provider => provider
                                                .GetService<IConfiguration>()
                                                .GetSection("LongPollServiceOptions")
                                                .Get<LongPollServiceOptions>()
                            );

                        collection.AddHostedService<LongPollService>();

                        collection
                            .AddDefaultAWSOptions()
                            .AddAWSService<IAmazonSQS>();
                    }
                );
    }
}
