using DistributedCacheExtensions.Local.Abstraction;
using System;

namespace DistributedCacheExtensions.Local.Internal
{
    internal class DateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset Now => DateTimeOffset.UtcNow;
    }
}
