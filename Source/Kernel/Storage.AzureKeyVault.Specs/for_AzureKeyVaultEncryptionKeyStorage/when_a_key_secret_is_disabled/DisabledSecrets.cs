// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Security.KeyVault.Secrets;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_a_key_secret_is_disabled;

public static class DisabledSecrets
{
    public static async Task Disable(SecretClient client, string name)
    {
        // The emulator does not honour a later update of the flag, so the secret is written again as a disabled version.
        var current = await client.GetSecretAsync(name);
        var disabled = new KeyVaultSecret(name, current.Value.Value);
        disabled.Properties.Enabled = false;
        await client.SetSecretAsync(disabled);
    }

    public static async Task<int> VersionCount(SecretClient client, string name)
    {
        var count = 0;
        await foreach (var _ in client.GetPropertiesOfSecretVersionsAsync(name))
        {
            count++;
        }

        return count;
    }
}
