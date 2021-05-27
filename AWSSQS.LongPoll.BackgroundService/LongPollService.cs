using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWSSQS.LongPoll.BackgroundService.Models.External;
using Microsoft.Extensions.Hosting;

namespace AWSSQS.LongPoll.BackgroundService
{
    public class LongPollService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly Events _events;
        private readonly Func<ReceiveMessageRequest> _configFactory;
        private readonly LongPollServiceOptions _options;
        private readonly IHostApplicationLifetime _appLifetime;

        public LongPollService(IAmazonSQS sqsClient, Events events, LongPollServiceOptions options, Func<ReceiveMessageRequest> configFactory, IHostApplicationLifetime appLifetime)
        {
            _sqsClient = sqsClient;
            _configFactory = configFactory;
            _events = events ?? new Events();
            _options = options ?? new LongPollServiceOptions();
            _appLifetime = appLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(_appLifetime.StopApplication);

            while (!stoppingToken.IsCancellationRequested)
            {
                var config = _configFactory.Invoke();

                var receiveMessageResponse = await
                                                _sqsClient
                                                    .ReceiveMessageAsync
                                                    (
                                                        config,
                                                        stoppingToken
                                                    );

                Task
                    .WaitAll
                    (
                        receiveMessageResponse
                            .Messages
                            .Select
                            (
                                async message => await 
                                                    await 
                                                        _events
                                                            .OnMessageReceived
                                                            (
                                                                message,
                                                                stoppingToken
                                                            )
                                                            .ContinueWith
                                                            (
                                                                async antecedent =>
                                                                {
                                                                    if (antecedent.Status == TaskStatus.RanToCompletion && antecedent.Result)
                                                                    {
                                                                        await 
                                                                            _sqsClient
                                                                                 .DeleteMessageAsync
                                                                                 (
                                                                                     new DeleteMessageRequest
                                                                                     (
                                                                                         config.QueueUrl,
                                                                                         message.ReceiptHandle
                                                                                     ),
                                                                                     stoppingToken
                                                                                 );
                                                                    }
                                                                    else if (antecedent.Exception != null)
                                                                    {
                                                                        var shouldContinue = await 
                                                                                                _events
                                                                                                    .OnException
                                                                                                    (
                                                                                                        message,
                                                                                                        antecedent.Exception,
                                                                                                        stoppingToken
                                                                                                    );
            
                                                                        if (!shouldContinue)
                                                                        {
                                                                            await 
                                                                                StopAsync(stoppingToken);
                                                                        }
                                                                    }
                                                                },
                                                                stoppingToken
                                                            )
                            )
                            .ToArray()
                    );

                await
                    Task
                        .Delay
                        (
                            _options.SleepIntervalMilliseconds,
                            stoppingToken
                        );
            }
        }
    }
}