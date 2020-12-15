using System;
using Amazon.SQS.Model;

namespace AWSSQS.LongPoll.BackgroundService.Models
{
    internal class SQSMessageException : Exception
    {
        public Message SQSMessage { get; }

        public SQSMessageException(Exception innerException, Message sqsMessage)
        : base("Failed to process message", innerException)
        {
            SQSMessage = sqsMessage;
        }
    }
}
