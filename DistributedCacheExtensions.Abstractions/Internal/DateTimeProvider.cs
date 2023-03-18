using System;

namespace DistributedCacheExtensions.Abstractions.Internal
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset Now => DateTimeOffset.UtcNow;
    }
}
