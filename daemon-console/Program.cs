// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates; //Only import this if you are using certificate
using System.Threading.Tasks;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using System.Threading;
using EnsureThat;

namespace daemon_console
{
    /// <summary>
    /// This sample shows how to query the Microsoft Graph from a daemon application
    /// which uses application permissions.
    /// For more information see https://aka.ms/msal-net-client-credentials
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                RunAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        private static async Task RunAsync()
        {
            AuthenticationConfig config = AuthenticationConfig.ReadFromJsonFile("appsettings.json");

            Console.WriteLine(config.ClientId);
            Console.WriteLine(config.ClientSecret);
            Console.WriteLine(config.Authority);
            // You can run this sample using ClientSecret or Certificate. The code will differ only when instantiating the IConfidentialClientApplication
            bool isUsingClientSecret = AppUsesClientSecret(config);

            // Even if this is a console application here, a daemon application is a confidential client application
            IConfidentialClientApplication app;

            if (isUsingClientSecret)
            {

                string keyVaultName = "tokenvalult";
                var kvUri = "https://" + keyVaultName + ".vault.azure.net";
                SecretClient client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());

                /* 
                KeyVDistCache cache = new KeyVDistCache(GetCacheOptions(client));
                await cache.RemoveAsync("ba5850f9-d3ed-4cc0-98a7-b6d0004a48e0-common-AppTokenCache");
                //byte[] res = cache.Get("nonexistingkey");
                */
                
                app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                    .WithClientSecret(config.ClientSecret)
                    .WithAuthority(new Uri(config.Authority))
                    .Build();
                
                // new code

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
                            //options.SlidingExpiration = TimeSpan.FromSeconds(15);
                        });
                });

            
           /*
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
    
     
                app.AddDistributedTokenCache(services =>
                {
                    // Requires to reference Microsoft.Extensions.Caching.Cosmos
                    services.AddCosmosCache((CosmosCacheOptions cacheOptions) =>
                    {
                    cacheOptions.ContainerName = "tokens";
                    cacheOptions.DatabaseName = "TokensDb";
                    cacheOptions.ClientBuilder = new CosmosClientBuilder("AccountEndpoint=https://dbstore.documents.azure.com:443/;AccountKey=ZmbhZbaGUBvu66hCxVL0jh1pV6tplDm6aJ6ioEagHy8wlpHEiDoVq6aU6s8asYvAuKDTxDha5XvaACDbgcOtqA==;");
                    cacheOptions.CreateIfNotExists = true;
                    });
                });
                */
          
            }
        


            else
            {
                X509Certificate2 certificate = ReadCertificate(config.CertificateName);
                app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                    .WithCertificate(certificate)
                    .WithAuthority(new Uri(config.Authority))
                    .Build();
            }

            // With client credentials flows the scopes is ALWAYS of the shape "resource/.default", as the 
            // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
            // a tenant administrator. 
            string[] scopes = new string[] { $"{config.ApiUrl}.default" }; 
            
            AuthenticationResult result = null;
            AuthenticationResult result3 = null;

            try
            {
                result = await app.AcquireTokenForClient(scopes)
                    .ExecuteAsync();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Token acquired");
                Console.ResetColor();


                Console.WriteLine(result.AccessToken);
                Console.WriteLine("======================");

                bool _quitFlag = false;
                Console.CancelKeyPress += delegate {
                _quitFlag = true;
                };

                // kick off asynchronous stuff 

                while (!_quitFlag) {

                    result3 = await app.AcquireTokenForClient(scopes)
                        .ExecuteAsync();
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                    Console.WriteLine("Token acquired 3rd time");
                    Console.ResetColor();
                    Console.WriteLine(result3.AccessToken);
                    Console.WriteLine(DateTime.Now);
                    Console.WriteLine("======================");
                    
                }
            }
            catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS70011"))
            {
                // Invalid scope. The scope has to be of the form "https://resourceurl/.default"
                // Mitigation: change the scope to be as expected
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Scope provided is not supported");
                Console.ResetColor();
            }

            if (result != null)
            {
                var httpClient = new HttpClient();
                var apiCaller = new ProtectedApiCallHelper(httpClient);
                await apiCaller.CallWebApiAndProcessResultASync($"{config.ApiUrl}v1.0/users", result.AccessToken, Display);
            }
        }

        /// <summary>
        /// Display the result of the Web API call
        /// </summary>
        /// <param name="result">Object to display</param>
        private static void Display(JObject result)
        {
            foreach (JProperty child in result.Properties().Where(p => !p.Name.StartsWith("@")))
            {
                Console.WriteLine($"{child.Name} = {child.Value}");
            }
        }

        /// <summary>
        /// Checks if the sample is configured for using ClientSecret or Certificate. This method is just for the sake of this sample.
        /// You won't need this verification in your production application since you will be authenticating in AAD using one mechanism only.
        /// </summary>
        /// <param name="config">Configuration from appsettings.json</param>
        /// <returns></returns>
        private static bool AppUsesClientSecret(AuthenticationConfig config)
        {
            string clientSecretPlaceholderValue = "[Enter here a client secret for your application]";
            string certificatePlaceholderValue = "[Or instead of client secret: Enter here the name of a certificate (from the user cert store) as registered with your application]";

            if (!String.IsNullOrWhiteSpace(config.ClientSecret) && config.ClientSecret != clientSecretPlaceholderValue)
            {
                return true;
            }

            else if (!String.IsNullOrWhiteSpace(config.CertificateName) && config.CertificateName != certificatePlaceholderValue)
            {
                return false;
            }

            else
                throw new Exception("You must choose between using client secret or certificate. Please update appsettings.json file.");
        }

        private static X509Certificate2 ReadCertificate(string certificateName)
        {
            if (string.IsNullOrWhiteSpace(certificateName))
            {
                throw new ArgumentException("certificateName should not be empty. Please set the CertificateName setting in the appsettings.json", "certificateName");
            }
            CertificateDescription certificateDescription = CertificateDescription.FromStoreWithDistinguishedName(certificateName);
            DefaultCertificateLoader defaultCertificateLoader = new DefaultCertificateLoader();
            defaultCertificateLoader.LoadIfNeeded(certificateDescription);
            return certificateDescription.Certificate;
        }

        private SecretClient GetSecretClient() {

            string keyVaultName = "tokenvalult";
            var kvUri = "https://" + keyVaultName + ".vault.azure.net";

            SecretClient client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());
            Console.WriteLine("KeyVDistCache constructor");

            return client;

        }


        private static KeyVDistCacheOptions GetCacheOptions(SecretClient client)
        {
            return new KeyVDistCacheOptions()
            {
                SecretClient = client
            };
        }
    }
}
