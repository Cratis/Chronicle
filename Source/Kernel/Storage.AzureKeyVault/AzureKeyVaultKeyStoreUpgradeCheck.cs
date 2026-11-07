// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Security.KeyVault.Secrets;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionKeyStoreUpgradeCheck"/> for Azure Key Vault.
/// </summary>
/// <remarks>
/// Names written by this version and names written by earlier versions never overlap, so there is no layout to
/// verify. Earlier names are counted from one listing. Which identifiers produced them cannot be told from the
/// names alone, so no unresolved shapes are reported.
/// </remarks>
/// <param name="secretClient">The <see cref="SecretClient"/> to list the secrets through.</param>
internal class AzureKeyVaultKeyStoreUpgradeCheck(SecretClient secretClient) : IEncryptionKeyStoreUpgradeCheck
{
    const string EarlierPrefix = "chronicle--";

    /// <inheritdoc/>
    public async Task<EncryptionKeyStoreUpgradeStatus> Inspect(bool full, CancellationToken cancellationToken = default)
    {
        var keys = 0L;
        var erasures = 0L;
        await foreach (var properties in secretClient.GetPropertiesOfSecretsAsync(cancellationToken))
        {
            if (!properties.Name.StartsWith(EarlierPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (properties.Name.EndsWith($"--{SecretLocations.ErasureSuffix}", StringComparison.OrdinalIgnoreCase))
            {
                erasures++;
            }
            else
            {
                keys++;
            }
        }

        return new(true, false, keys, erasures, 0, true);
    }
}
