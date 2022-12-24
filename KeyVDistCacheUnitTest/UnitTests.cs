using Azure.Security.KeyVault.Secrets;
using Moq;
using Azure;
using NSubstitute;

public class UnitTests
{

    Mock<SecretClient> clientMock = new Mock<SecretClient>();

    [SetUp]
    public void Setup()
    {


    }


    [Test]
    public void Verify_GetSyncReturns_EncodedString_IfSecretExists()
    {

        String secretValueEncoded = "c2VjcmV0VmFsdWU=";
        KeyVaultSecret secret1 = SecretModelFactory.KeyVaultSecret(new SecretProperties("secret"), secretValueEncoded);
      
        Response<KeyVaultSecret> response = Response.FromValue(secret1, Mock.Of<Response>());
        clientMock.Setup(c => c.GetSecret(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns( response);
        
        KeyVDistCache cache = new KeyVDistCache(GetCacheOptions(clientMock.Object));
        Assert.AreEqual(Convert.ToBase64String(cache.Get("key")),secretValueEncoded);
    }

    [Test]
    public void Verify_GetSyncReturns_Throws_IfSecretIsAbsent()
    {

        String secretValueEncoded = "c2VjcmV0VmFsdWU=";
        KeyVaultSecret secret1 = SecretModelFactory.KeyVaultSecret(new SecretProperties("secret"), secretValueEncoded);
      
        
        Response<KeyVaultSecret> response = Response.FromValue(secret1, Mock.Of<Response>());
        SecretClient secretClientSubstitue = Substitute.For<SecretClient>();
        secretClientSubstitue.GetSecret(Arg.Any<String>()).Returns(response);

    
        KeyVDistCache cache = new KeyVDistCache(GetCacheOptions(secretClientSubstitue));
        Assert.AreEqual(Convert.ToBase64String(cache.Get("key")),secretValueEncoded);
    }

    private static KeyVDistCacheOptions GetCacheOptions(SecretClient client)
    {
        return new KeyVDistCacheOptions()
        {
            SecretClient = client
        };
    }

    public static string Base64Encode(string plainText) {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
    }
}
