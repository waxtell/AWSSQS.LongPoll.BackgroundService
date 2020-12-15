using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWSSQS.LongPoll.BackgroundService.Extensions;
using AWSSQS.LongPoll.BackgroundService.Models;
using AWSSQS.LongPoll.BackgroundService.Models.External;

namespace AWSSQS.LongPoll.BackgroundService
{
    public class LongPollService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly Events _events;
        private readonly Func<ReceiveMessageRequest> _configFactory;
        private readonly LongPollServiceOptions _options;

        public LongPollService(IAmazonSQS sqsClient, Events events, LongPollServiceOptions options, Func<ReceiveMessageRequest> configFactory)
        {
            _sqsClient = sqsClient;
            _configFactory = configFactory;
            _events = events ?? new Events();
            _options = options ?? new LongPollServiceOptions();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
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
                                    message => _events
                                        .OnMessageReceived
                                        (
                                            message,
                                            stoppingToken
                                        )
                                        .ContinueWith
                                        (
                                            antecedent =>
                                            {
                                                if (antecedent.Status == TaskStatus.RanToCompletion && antecedent.Result)
                                                {
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
                                                    throw new SQSMessageException(antecedent.Exception, message);
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
                catch (AggregateException aggregateException)
                {
                    var shouldContinue = await 
                                            aggregateException
                                                .InnerExceptions
                                                .OfType<SQSMessageException>()
                                                .Aggregate
                                                (
                                                    true, 
                                                    async (current, exception) => current && await _events.OnException(exception.SQSMessage, exception.InnerException, stoppingToken)
                                                );

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