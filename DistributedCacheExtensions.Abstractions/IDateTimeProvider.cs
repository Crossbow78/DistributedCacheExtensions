using System;

namespace DistributedCacheExtensions.Abstractions
{
    public interface IDateTimeProvider
    {
        DateTimeOffset Now { get; }
    }
}