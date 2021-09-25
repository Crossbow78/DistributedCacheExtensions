# DistributedCacheExtensions
This library provides an implementation of `Microsoft.Extensions.Caching.Distributed.IDistributedCache` that is based on the local file system.

It was created to fill that development hole between `AddDistributedMemoryCache` (which is not preserved between app invocations)
and `AddStackExchangeRedisCache` or `AddDistributedSqlServerCache` (which both require additional setup, configuration and security of external resources).

# Usage
Configure the dependency during startup with the following extension method:
```csharp
    public void SetupServices(IServiceCollection services)
    {
        services.AddDistributedFileCache(options =>
        {
            options.Path = @"D:\Path\To\Cache\";
            options.MetadataHandler = DistributedCacheExtensions.MetadataHandler.SeparateFile;
        });
    }
```

Then use it like any `IDistributedCache` in your service, for example:
```csharp
    public class MyService
    {
        private readonly IDistributedCache _distributedCache;

        public MyService(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        }

        public async Task<string> GetData(string key)
        {
            var cachedContent = await _distributedCache.GetStringAsync(key);
            if (cachedContent != null)
            {
                return cachedContent;
            }
            
            var content = await FetchDataFromApi(key);
            await _distributedCache.SetStringAsync(key, content, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60),
                SlidingExpiration = TimeSpan.FromSeconds(10),
            });
        }
        
        public async Task<string> FetchDataFromApi(string key)
        {
            // (...)
        }
    }
```

# Configuration
Each cached value is stored in a separate file in a folder on the file system. The location of this cache folder must be defined in the configuration.
The expiration parameters are saved separately, depending on which metadata handler is selected.

#### MetadataHandler.SeparateFile
By default, the file-based metadata handler is selected. This handler writes a second file with ".metadata" extension for each cached value, which
contains the json encoded expiration parameters.

#### MetadataHandler.Timestamps
Alternatively, a timestamp metadata handler can be selected. This will encode the expiration using the created/modified/accessed timestamps of the file system.
Note that this will only fully work if the operating system does not automatically update any of these timestamps.
On Windows this can be achieved by executing the following command on an elevated command prompt:
```cmd
fsutil behavior set disablelastaccess 0x3
```

