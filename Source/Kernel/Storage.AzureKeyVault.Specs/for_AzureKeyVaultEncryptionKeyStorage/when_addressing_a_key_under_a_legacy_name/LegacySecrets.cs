// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name;

/// <summary>
/// Writes and probes secrets under the names keys were stored under before names were hashed.
/// </summary>
public static class LegacySecrets
{
    public static string Name(string eventStore, string identifier, string suffix, string eventStoreNamespace = "default") =>
        $"chronicle--{eventStore}--{eventStoreNamespace}--{identifier}--{suffix}";

    public static async Task WriteKey(SecretClient client, string name, EncryptionKey key) =>
        await client.SetSecretAsync(name, JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["publicKey"] = Convert.ToBase64String(key.Public),
            ["privateKey"] = Convert.ToBase64String(key.Private)
        }));

    public static async Task WriteErasure(SecretClient client, string name, EncryptionKey erasedKey) =>
        await client.SetSecretAsync(name, JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["erasedThrough"] = "1",
            ["erasedKeyFingerprints"] = erasedKey.Fingerprint,
            ["newKeyAllowed"] = bool.FalseString
        }));

    public static async Task<bool> IsLive(SecretClient client, string name)
    {
        try
        {
            await client.GetSecretAsync(name);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }

    public static async Task<bool> IsSoftDeleted(SecretClient client, string name)
    {
        try
        {
            await client.GetDeletedSecretAsync(name);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }
}
