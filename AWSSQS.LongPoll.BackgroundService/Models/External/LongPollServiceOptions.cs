namespace AWSSQS.LongPoll.BackgroundService.Models.External
{
    public class LongPollServiceOptions
    {
        public int SleepIntervalMilliseconds { get; set; } = 10000;
    }
}
