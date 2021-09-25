using DistributedCacheExtensions.Abstraction;
using System;

namespace DistributedCacheExtensions.Internal
{
    internal class DateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset Now => DateTimeOffset.UtcNow;
    }
}
