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
/// verify. Earlier names are counted from one listing. When asked for a full inspection and the scopes an operator
/// listed tie a name to one address, an earlier secret whose copy under the current name provably holds the same value
/// is counted as retained, not as pending. Every other earlier name stays pending.
/// </remarks>
/// <param name="vault">The <see cref="ISecretVault"/> to read the secrets through.</param>
/// <param name="scopes">The event store and namespace pairs an operator listed as holding keys under earlier names.</param>
internal class AzureKeyVaultKeyStoreUpgradeCheck(ISecretVault vault, IReadOnlyList<LegacyScope> scopes) : IEncryptionKeyStoreUpgradeCheck
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultKeyStoreUpgradeCheck"/> class that does not tie names to addresses.
    /// </summary>
    /// <param name="secretClient">The <see cref="SecretClient"/> to list the secrets through.</param>
    public AzureKeyVaultKeyStoreUpgradeCheck(SecretClient secretClient)
        : this(new SecretClientVault(secretClient), [])
    {
    }

    /// <inheritdoc/>
    public async Task<EncryptionKeyStoreUpgradeStatus> Inspect(bool full, CancellationToken cancellationToken = default)
    {
        var keys = 0L;
        var erasures = 0L;
        var retained = 0L;
        foreach (var properties in (await vault.ListLive(cancellationToken)).Where(_ => LegacySecretName.IsEarlier(_.Name)))
        {
            if (full && await IsRetained(properties, cancellationToken))
            {
                retained++;
            }
            else if (properties.Name.EndsWith($"--{SecretLocations.ErasureSuffix}", StringComparison.OrdinalIgnoreCase))
            {
                erasures++;
            }
            else
            {
                keys++;
            }
        }

        return new(true, false, keys, erasures, 0, true, retained);
    }

    async Task<bool> IsRetained(SecretVersionInfo properties, CancellationToken cancellationToken)
    {
        if (LegacySecretName.Resolve(properties.Name, scopes) is not { } resolved)
        {
            return false;
        }

        var versions = await vault.ListVersions(properties.Name, cancellationToken);
        if (versions.Count != 1 || versions[0].HasAttributes)
        {
            return false;
        }

        var source = await vault.Read(properties.Name, versions[0].Version, cancellationToken);
        return source is not null && await DestinationCheck.EqualsSource(vault, resolved.Destination, source, cancellationToken);
    }
}
