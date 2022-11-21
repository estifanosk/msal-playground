

using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Internal;
using System.Collections.Generic;
using Azure;
using EnsureThat;


public class KeyVDistCache : IDistributedCache
    {

    private readonly ISystemClock _systemClock;

        private SecretClient _client;

        public KeyVDistCache(IOptions<KeyVDistCacheOptions> options)
        {
            EnsureArg.IsNotNull(options,nameof(options));
            EnsureArg.IsNotNull(options.Value,nameof(options.Value));
            EnsureArg.IsNotNull(options.Value.SecretClient,nameof(KeyVDistCacheOptions.SecretClient));

            this._client = options.Value.SecretClient;
            this._systemClock = new SystemClock();
        }


        public byte[] Get(string key)
        {
            EnsureArg.IsNotNullOrWhiteSpace(key,nameof(key));

  
            KeyVaultSecret secret =_client.GetSecret(mapKey(key));

            if ( secret == null) {
                Console.WriteLine("Get secret not found in keyvault");
                return null;
            }

            if (secret.Properties.ExpiresOn<=_systemClock.UtcNow) {
                Console.WriteLine("access token has expired.");
                return null;
            }

            return Convert.FromBase64String(secret.Value);
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
        {

            EnsureArg.IsNotNullOrWhiteSpace(key, nameof(key));

            token.ThrowIfCancellationRequested();

            Console.WriteLine("GetAsync,key" + key);
            KeyVaultSecret secret = await _client.GetSecretAsync(mapKey(key), String.Empty,token);
        
            if (secret == null) {
                Console.WriteLine("Get secret not found in keyvault");
                return null;
            }

            if (secret.Properties.ExpiresOn<=_systemClock.UtcNow) {
                Console.WriteLine("access token has expired.");
                return null;
            }

            Console.WriteLine("GetASync secret retrieved:" + secret.Value);
            return Convert.FromBase64String(secret.Value);
        }

        public void Refresh(string key)
        {
            throw new NotImplementedException();
        }

        public async Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            await Task.Run(() => throw new NotImplementedException());
        }

        public void Remove(string key)
        {
            EnsureArg.IsNotNullOrWhiteSpace(key, nameof(key));

            Console.WriteLine("Remove called.");
            DeleteSecretOperation operation = _client.StartDeleteSecret(mapKey(key));
            operation.WaitForCompletion();
            _client.PurgeDeletedSecret(mapKey(key));
        }

        public async Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
        {
            EnsureArg.IsNotNullOrWhiteSpace(key, nameof(key));

            Console.WriteLine("RemoveAsync invoked.");

            DeleteSecretOperation operation = await _client.StartDeleteSecretAsync(mapKey(key),token);
            var response = await operation.WaitForCompletionAsync(token);
            await _client.PurgeDeletedSecretAsync(mapKey(key),token);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            EnsureArg.IsNotNullOrWhiteSpace(key, nameof(key));

            Console.WriteLine("Set called.");
            Console.WriteLine("Set, absolute exp:" + options.ToString());
            KeyVaultSecret secret = new KeyVaultSecret(mapKey(key), Convert.ToBase64String(value));
            secret.Properties.ExpiresOn = options.AbsoluteExpiration;

            Remove(key);
            _client.SetSecret(secret,CancellationToken.None);
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
        {
            EnsureArg.IsNotNullOrWhiteSpace(key, nameof(key));

            Console.WriteLine("SetAsync called.");
            KeyVaultSecret secret = new KeyVaultSecret(mapKey(key), Convert.ToBase64String(value));
            secret.Properties.ExpiresOn = options.AbsoluteExpiration;

            await RemoveAsync(key);
            await _client.SetSecretAsync(secret, token);
        }

        private String mapKey(String key) {
            return key.Replace("_","-");
        }
    }