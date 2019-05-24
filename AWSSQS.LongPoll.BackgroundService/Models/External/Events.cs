﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;

namespace AWSSQS.LongPoll.BackgroundService.Models.External
{
    public class Events
    {
        public Func<Message, CancellationToken, Task> OnMessageReceived { get; set; } = async (message, cancellationToken) => await Task.CompletedTask;
        public Func<Exception, CancellationToken, Task<bool>> OnException { get; set; } = async (exception, cancellationToken) => await Task.FromException<bool>(exception);
    }
}
