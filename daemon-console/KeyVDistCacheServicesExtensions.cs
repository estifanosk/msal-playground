
using Microsoft.Extensions.Caching.Distributed;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class KeyVDistCacheServicesExtensions
    {
        public static IServiceCollection AddDistributedKeyVaultCache(this IServiceCollection services, Action<KeyVDistCacheOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.AddOptions();
            services.AddSingleton<IDistributedCache, KeyVDistCache>();
            services.Configure(setupAction);

            return services;
        }
    }
}