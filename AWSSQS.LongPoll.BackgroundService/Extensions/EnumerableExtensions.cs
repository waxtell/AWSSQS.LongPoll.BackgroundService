using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AWSSQS.LongPoll.BackgroundService.Extensions
{
    internal static class EnumerableExtensions
    {
        public static async Task<TAccumulate> Aggregate<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, Task<TAccumulate>> func)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            var accumulate = seed;

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var item in source)
            {
                accumulate = await func(accumulate, item);
            }

            return accumulate;
        }
    }
}