using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;

namespace AWSSQS.LongPoll.BackgroundService.Models.External
{
    public class Events
    {
        public Func<Message, CancellationToken, Task<bool>> OnMessageReceived { get; set; } = async (message, cancellationToken) => await Task.FromResult(true);
        public Func<Message, Exception, CancellationToken, Task<bool>> OnException { get; set; } = async (message, exception, cancellationToken) => await Task.FromException<bool>(exception);
    }
}
