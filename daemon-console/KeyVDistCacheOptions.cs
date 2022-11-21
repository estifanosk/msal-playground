using Microsoft.Extensions.Options;
using Azure.Security.KeyVault.Secrets;

public class KeyVDistCacheOptions: IOptions<KeyVDistCacheOptions>
{


    /// <summary>
    /// The key vault secret client 
    /// </summary>
    public SecretClient SecretClient { get; set; }



    KeyVDistCacheOptions IOptions<KeyVDistCacheOptions>.Value
    {
        get
        {
            return this;
        }
    }
}
