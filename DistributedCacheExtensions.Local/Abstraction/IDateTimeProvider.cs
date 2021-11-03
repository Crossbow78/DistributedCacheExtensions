using System;

namespace DistributedCacheExtensions.Local.Abstraction
{
    internal interface IDateTimeProvider
    {
        DateTimeOffset Now { get; }
    }
}