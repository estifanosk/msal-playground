// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using System.Threading;


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
            var serviceCollection = new ServiceCollection();
            //serviceCollection.AddJwtFuncCosmosDb(config);
            serviceCollection.AddJwtFuncKeyvault(config);

            var services = serviceCollection.BuildServiceProvider();

            JwtFunc jwtFunc = services.GetService<JwtFunc>();


            // With client credentials flows the scopes is ALWAYS of the shape "resource/.default", as the 
            // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
            // a tenant administrator. 
            string[] scopes = new string[] { $"{config.ApiUrl}.default" }; 
            
            AuthenticationResult result = null;
            int n = 0;
            try
            {
                bool _quitFlag = false;
                Console.CancelKeyPress += delegate {
                _quitFlag = true;
                };

                // kick off asynchronous stuff 

                while (!_quitFlag) {

                    //result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
                    result = await jwtFunc(config.ApiUrl);

                    Console.WriteLine($"Token acquired ${n} th time");
                    Util.PrintMetrics(result.AuthenticationResultMetadata);
                    Console.ResetColor();
                    Console.WriteLine(result.AccessToken);
                    Console.WriteLine(DateTime.Now);

                    if (result != null)
                        {
                            var httpClient = new HttpClient();
                            var apiCaller = new ProtectedApiCallHelper(httpClient);
                            await apiCaller.CallWebApiAndProcessResultASync($"{config.ApiUrl}v1.0/users", result.AccessToken, Util.Display);
                        }
                    Console.WriteLine("======================");
                    Thread.Sleep(TimeSpan.FromSeconds(60));
                    n++;
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
        }
    }
}
