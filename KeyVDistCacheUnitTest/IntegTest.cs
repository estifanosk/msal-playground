using Azure.Security.KeyVault.Secrets;
using Moq;
using Azure.Identity;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Internal;
using System.Collections.Generic;
using  System.Text;
using Azure;

public class IntegTests
{

    KeyVDistCache cache;
    SecretClient client;
    

    [SetUp]
    public void Setup()
    {
        string keyVaultName = "tokenvalult";
        var kvUri = "https://" + keyVaultName + ".vault.azure.net";
        client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());
        cache = new KeyVDistCache(GetCacheOptions(client));
    }


    [Test]
    public async Task Verify_GetASyncReturns_EncodedString_IfSecretExists()
    {

        String key = "client-id-0-0";
        String secretValueEncoded = "c2VjcmV0VmFsdWU=";

        DistributedCacheEntryOptions options = new DistributedCacheEntryOptions();
        options.AbsoluteExpiration = DateTimeOffset.Now + TimeSpan.FromDays(1);
        await cache.SetAsync(key,Convert.FromBase64String(secretValueEncoded),options);

        byte[] result = await cache.GetAsync(key);
        Assert.AreEqual(Convert.ToBase64String(result), secretValueEncoded);


        await cache.RemoveAsync(key);

        RequestFailedException ex = Assert.ThrowsAsync<RequestFailedException>(() => cache.GetAsync(key));
        Assert.AreEqual( 404, ex.Status);
    }

    [Test]
    public async Task Verify_Get_Returns_EncodedString_IfSecretExists()
    {

        String key = "client-id2-0-0";
        String secretValueEncoded = "c2VjcmV0VmFsdWU=";

        DistributedCacheEntryOptions options = new DistributedCacheEntryOptions();
        options.AbsoluteExpiration = DateTimeOffset.Now + TimeSpan.FromDays(1);
        await cache.SetAsync(key,Convert.FromBase64String(secretValueEncoded),options);

        byte[] result = cache.Get(key);
        Assert.AreEqual(Convert.ToBase64String(result), secretValueEncoded);


        cache.Remove(key);

        RequestFailedException ex = Assert.Throws<RequestFailedException>(() => cache.Get(key));
        Assert.AreEqual( 404, ex.Status);
    }

    private void Remove(string key)
    {
        Console.WriteLine("Remove called.");
        DeleteSecretOperation operation = client.StartDeleteSecret(key);
        operation.WaitForCompletion();
        client.PurgeDeletedSecret(key);
    }

    private static KeyVDistCacheOptions GetCacheOptions(SecretClient client)
    {
        return new KeyVDistCacheOptions()
        {
            SecretClient = client
        };
    }

    

    public static string Base64Encode(string plainText) {
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
    }
}
