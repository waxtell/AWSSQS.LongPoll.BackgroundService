using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWSSQS.LongPoll.BackgroundService.Models.External;

namespace AWSSQS.LongPoll.BackgroundService
{
    public class LongPollService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly Events _events;
        private readonly Func<ReceiveMessageRequest> _requestFactory;
        private readonly LongPollServiceOptions _options;

        public LongPollService(IAmazonSQS sqsClient, Events events, LongPollServiceOptions options, Func<ReceiveMessageRequest> requestFactory)
        {
            _sqsClient = sqsClient;
            _requestFactory = requestFactory;
            _events = events ?? new Events();
            _options = options ?? new LongPollServiceOptions();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var receiveMessageResponse = await 
                                                    _sqsClient
                                                        .ReceiveMessageAsync
                                                        (
                                                            _requestFactory.Invoke(), 
                                                            stoppingToken
                                                        );

                    Task
                        .WaitAll
                        (
                            receiveMessageResponse
                                    .Messages
                                    .Select
                                    (
                                        message => _events
                                                        .OnMessageReceived
                                                        (
                                                            message,
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
                catch (TaskCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    var shouldContinue = await
                                            _events
                                                .OnException(e, stoppingToken);

                    if (!shouldContinue)
                    {
                        await
                            StopAsync(stoppingToken);
                    }
                }
            }
        }
    }
}