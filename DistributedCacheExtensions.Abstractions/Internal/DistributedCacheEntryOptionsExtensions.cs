using Microsoft.Extensions.Caching.Distributed;
using System;

namespace DistributedCacheExtensions.Abstractions.Internal
{
    public static class DistributedCacheEntryOptionsExtensions
    {
        internal static DateTime? GetAbsoluteExpiration(this DistributedCacheEntryOptions options)
        {
            var absoluteExpiration = options.AbsoluteExpirationRelativeToNow.HasValue
                ? DateTime.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value)
                : (DateTime?)null;

            if (options.AbsoluteExpiration.HasValue)
                absoluteExpiration = options.AbsoluteExpiration.Value.UtcDateTime;

            return absoluteExpiration;
        }
    }
}
