using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace DistributedFileCacheExample.ConsoleApp
{
    public class MyService : IMyService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<MyService> _logger;

        public MyService(IDistributedCache distributedCache, ILogger<MyService> logger)
        {
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string DoSomething(string key)
        {
            var cache = _distributedCache.GetString(key);
            if (cache != null)
            {
                _logger.LogInformation("Cached value");
                return cache;
            }

            _logger.LogInformation("Fresh value");
            var guid = Guid.NewGuid().ToString();
            _distributedCache.SetString(key, guid, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60),
                SlidingExpiration = TimeSpan.FromSeconds(10),
            });
            return guid;
        }

        public void Reset(string key)
        {
            _distributedCache.Remove(key);
        }
    }
}
