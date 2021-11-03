using System;

namespace DistributedCacheExtensions.Local.Abstraction
{
    internal interface ICacheMetadata
    {
        DateTime? AbsoluteExpiration { get; set; }
        string Reference { get; set; }
        string Key { get; set; }
        TimeSpan? SlidingExpiration { get; set; }
        DateTime? SlidingExpirationMoment { get; set; }
    }
}