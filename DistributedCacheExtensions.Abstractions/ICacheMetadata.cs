using System;

namespace DistributedCacheExtensions.Abstractions
{
    public interface ICacheMetadata
    {
        DateTime? AbsoluteExpiration { get; set; }
        string Reference { get; set; }
        string Key { get; set; }
        TimeSpan? SlidingExpiration { get; set; }
        DateTime? SlidingExpirationMoment { get; set; }
    }
}