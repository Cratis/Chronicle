// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Storage.Compliance;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.AuthMethods.Token;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.given;

/// <summary>
/// Reads and writes secrets at exact paths, bypassing the key store, to stand in for keys written by earlier versions.
/// </summary>
/// <param name="address">The Vault address.</param>
public class raw_vault(string address)
{
    readonly VaultClient _client = new(new VaultClientSettings(address, new TokenAuthMethodInfo(VaultFixture.RootToken)));

    public async Task WriteKey(string path, EncryptionKey key) =>
        await _client.V1.Secrets.KeyValue.V2.WriteSecretAsync(
            path,
            new Dictionary<string, object>
            {
                ["publicKey"] = Convert.ToBase64String(key.Public),
                ["privateKey"] = Convert.ToBase64String(key.Private)
            },
            mountPoint: "secret");

    public async Task<bool> Exists(string path)
    {
        try
        {
            await _client.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, mountPoint: "secret");
            return true;
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
