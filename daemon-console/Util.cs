using System;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using System.Threading;
namespace daemon_console
{
	public static class Util
	{

        /// <summary>
        /// Display the result of the Web API call
        /// </summary>
        /// <param name="result">Object to display</param>
        public static void Display(JObject result)
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
        public static bool AppUsesClientSecret(AuthenticationConfig config)
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



        public static void PrintMetrics(AuthenticationResultMetadata data)
        {
            Console.WriteLine("TokenSource: " + data.TokenSource);
            Console.WriteLine("CacheRefreshReason: " + data.CacheRefreshReason);
            Console.WriteLine("TotalDurationInMs : " + data.DurationTotalInMs);
            Console.WriteLine("DurationInCacheInMs: " + data.DurationInCacheInMs);
            Console.WriteLine("DurationInHttpInMs: " + data.DurationInHttpInMs);

        }
	}
}

