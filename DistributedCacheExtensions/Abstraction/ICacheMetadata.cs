using System;
using System.IO.Abstractions;

namespace DistributedCacheExtensions.Abstraction
{
    internal interface ICacheMetadata
    {
        DateTime? AbsoluteExpiration { get; set; }
        IFileInfo FileInfo { get; set; }
        string Key { get; set; }
        TimeSpan? SlidingExpiration { get; set; }
        DateTime? SlidingExpirationMoment { get; set; }
    }
}