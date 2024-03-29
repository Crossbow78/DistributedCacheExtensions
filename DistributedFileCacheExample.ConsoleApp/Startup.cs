﻿using Microsoft.Extensions.DependencyInjection;
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
                .AddDistributedFileCache(x =>
                {
                    x.Path = @"D:\Dev\Temp\Cache\";
                    //x.MetadataHandler = DistributedCacheExtensions.MetadataHandler.SeparateFile;
                })
                .AddLogging(x => x
                    .SetMinimumLevel(LogLevel.Debug)
                    .AddSimpleConsole(z => z.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] "))
                .AddScoped<IMyService, MyService>();
        }
    }
}


