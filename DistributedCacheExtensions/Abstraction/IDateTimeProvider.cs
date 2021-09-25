using System;

namespace DistributedCacheExtensions.Abstraction
{
    internal interface IDateTimeProvider
    {
        DateTimeOffset Now { get; }
    }
}