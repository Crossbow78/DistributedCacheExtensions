using DistributedCacheExtensions.Local;
using DistributedCacheExtensions.Local.Abstraction;
using DistributedCacheExtensions.Local.Internal;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDistributedFileCache(this IServiceCollection services)
        {
            services.AddDistributedFileCacheInternal();
            services.AddMetadataHandler();
            return services;
        }

        public static IServiceCollection AddDistributedFileCache(this IServiceCollection services, Action<DistributedFileCacheOptions> setupAction)
        {
            services.AddDistributedFileCacheInternal();
            services.AddMetadataHandler(setupAction);
            services.Configure(setupAction);
            return services;
        }

        private static IServiceCollection AddDistributedFileCacheInternal(this IServiceCollection services)
        {
            services.AddOptions();
            services.TryAdd(ServiceDescriptor.Singleton<IDistributedCache, DistributedFileCache>());
            services.TryAdd(ServiceDescriptor.Singleton<IStorageHandler, FileStorageHandler>());
            services.TryAdd(ServiceDescriptor.Singleton<IFileSystem, FileSystem>());
            services.TryAdd(ServiceDescriptor.Singleton<IDateTimeProvider, DateTimeProvider>());
            return services;
        }

        private static IServiceCollection AddMetadataHandler(this IServiceCollection services, Action<DistributedFileCacheOptions> setupAction = null)
        {
            var config = new DistributedFileCacheOptions();
            setupAction?.Invoke(config);
            if (config.MetadataHandler == MetadataHandler.SeparateFile)
            {
                services.TryAdd(ServiceDescriptor.Singleton<IMetadataHandler, FileMetadataHandler>());
            } 
            else
            {
                services.TryAdd(ServiceDescriptor.Singleton<IMetadataHandler, TimestampMetadataHandler>());
            }
            return services;
        }

    }
}
