
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using System;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Azure.Identity;
using Microsoft.Extensions.Caching.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.AspNetCore.DataProtection;
using Azure.Security.KeyVault.Secrets;

namespace daemon_console
{
    public static class JwtExt
    {
        public static IServiceCollection AddJwtFuncCosmosDb(this IServiceCollection services, AuthenticationConfig config)
        {
            services.AddSingleton<JwtFunc>(sp =>
            {
                string S1 ="DefaultEndpointsProtocol=https;AccountName=eskstore;AccountKey=iD7y2dRUZZvWqSwUF8QKbjF2WA/JGwN3McFbFKYc3uGuEYcUIy4a335FXTNqlT9myRfpEVcvYCnw+AStjYF7GA==;EndpointSuffix=core.windows.net";
 
                IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                    .WithClientSecret(config.ClientSecret)
                    .WithAuthority(new Uri(config.Authority))
                    //.WithLogging(myLogger)
                    .Build();

                app.AddDistributedTokenCache(services =>
                {
                    // Requires to reference Microsoft.Extensions.Caching.Cosmos
                    services.AddCosmosCache((CosmosCacheOptions cacheOptions) =>
                    {
                     cacheOptions.ContainerName = "tokensencrypted";
                    //cacheOptions.ContainerName = "tokens";
                    cacheOptions.DatabaseName = "TokensDb";
                    cacheOptions.ClientBuilder = new CosmosClientBuilder("AccountEndpoint=https://dbstore.documents.azure.com:443/;AccountKey=ZmbhZbaGUBvu66hCxVL0jh1pV6tplDm6aJ6ioEagHy8wlpHEiDoVq6aU6s8asYvAuKDTxDha5XvaACDbgcOtqA==;");
                    cacheOptions.CreateIfNotExists = false;
    
                    });

                    services
                        .AddDataProtection()
                        .ProtectKeysWithAzureKeyVault(new Uri("https://tokenvalult.vault.azure.net/keys/token-enc/b4f684354e0a4c31ad56b49b6307021a"), new DefaultAzureCredential())
                        .PersistKeysToAzureBlobStorage(S1,"my1stblob","keymat");
                        
                    services.Configure<MsalDistributedTokenCacheAdapterOptions>(options => 
                        {
                            // Optional: Disable the L1 cache in apps that don't use session affinity
                            //                 by setting DisableL1Cache to 'true'.
                            options.DisableL1Cache = false;
                            
                            // Or limit the memory (by default, this is 500 MB)
                            options.L1CacheOptions.SizeLimit = 1024 * 1024 * 1024; // 1 GB

                            // You can choose if you encrypt or not encrypt the cache
                            options.Encrypt = true;


                            // And you can set eviction policies for the distributed
                            // cache.
                            //options.SlidingExpiration = TimeSpan.FromHours(12);
                        });
                });

                //string[] scopes = new string[] { $"{config.ApiUrl}.default" };

                return async audience => 
                {
                    AuthenticationResult result = await app.AcquireTokenForClient(new string[] { $"{audience}.default" }).ExecuteAsync();
                    return result;
                };

            });

            return services;
        }


        public static IServiceCollection AddJwtFuncKeyvault(this IServiceCollection services, AuthenticationConfig config)
        {
            services.AddSingleton<JwtFunc>(sp =>
            {
                string keyVaultName = "tokenvalult";
                var kvUri = "https://" + keyVaultName + ".vault.azure.net";
                SecretClient client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());

                IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                    .WithClientSecret(config.ClientSecret)
                    .WithAuthority(new Uri(config.Authority))
                    //.WithLogging(myLogger)
                    .Build();

                app.AddDistributedTokenCache(services => {

                    Console.WriteLine("Initializing distributed cache");
                    //services.AddSingleton<Func<SecretClient>>(()=> client);
                    //services.AddSingleton<IDistributedCache,KeyVDistCache>();    

                    services.AddDistributedKeyVaultCache(c =>
                    {
                        c.SecretClient = client;
                    });

                    // Distributed token caches have a L1/L2 mechanism.
                    // L1 is in memory, and L2 is the distributed cache
                    // implementation that you will choose below.
                    // You can configure them to limit the memory of the 
                    // L1 cache, encrypt, and set eviction policies.
                    services.Configure<MsalDistributedTokenCacheAdapterOptions>(options =>
                    {
                        // Optional: Disable the L1 cache in apps that don't use session affinity
                        //                 by setting DisableL1Cache to 'true'.
                        options.DisableL1Cache = false;

                        // Or limit the memory (by default, this is 500 MB)
                        options.L1CacheOptions.SizeLimit = 1024 * 1024 * 1024; // 1 GB

                        // You can choose if you encrypt or not encrypt the cache
                        options.Encrypt = false;


                        // And you can set eviction policies for the distributed
                        // cache.
                        //options.SlidingExpiration = TimeSpan.FromHours(12);
                    });
                });

                //string[] scopes = new string[] { $"{config.ApiUrl}.default" };

                return async audience =>
                {
                    AuthenticationResult result = await app.AcquireTokenForClient(new string[] { $"{audience}.default" }).ExecuteAsync();
                    return result;
                };

            });

            return services;
        }
    }
}

/*
private SecretClient GetSecretClient()
{

    string keyVaultName = "tokenvalult";
    var kvUri = "https://" + keyVaultName + ".vault.azure.net";

    SecretClient client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());
    Console.WriteLine("KeyVDistCache constructor");

    return client;

}


// Add an in-memory token cache with options
        app.AddInMemoryTokenCache(services =>
        {
            // Configure the memory cache options
            services.Configure<MemoryCacheOptions>(options =>
            {
                options.SizeLimit = 500 * 1024 * 1024; // in bytes (500 MB)
            });
        }
        );

*/