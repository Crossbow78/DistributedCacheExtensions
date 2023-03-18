using DistributedCacheExtensions.Local;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace DistributedFileCacheExample.ConsoleApp
{
    public static class Startup
    {
        public static IServiceProvider GetServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            SetupServices(serviceCollection);
            return serviceCollection.BuildServiceProvider();
        }

        public static IServiceCollection SetupServices(IServiceCollection services)
        {
            return services
                //.AddDistributedMemoryCache()
                .AddDistributedFileCache(options =>
                {
                    options.Path = @"D:\Dev\Temp\Cache\";
                    //options.Path = @"/mnt/d/Dev/Temp/Cache/";
                    options.MetadataHandler = MetadataHandler.SeparateFile;
                })
                .AddLogging(x => x
                    .SetMinimumLevel(LogLevel.Debug)
                    .AddSimpleConsole(z => z.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] "))
                .AddScoped<IMyService, MyService>();
        }
    }
}


