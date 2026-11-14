// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// Decides whether a secret under a current name can be trusted to hold exactly what an earlier secret holds.
/// </summary>
internal static class DestinationCheck
{
    /// <summary>
    /// Checks that every version of the destination is a plain, enabled value equal to the source, so whichever version a reader selects equals the source.
    /// Anything else, including an unreadable version, fails closed.
    /// </summary>
    /// <param name="vault">The <see cref="ISecretVault"/>.</param>
    /// <param name="destination">The name of the secret under the current name.</param>
    /// <param name="source">The <see cref="SecretValue"/> of the secret under the earlier name.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>True when the destination provably equals the source.</returns>
    public static async Task<bool> EqualsSource(ISecretVault vault, string destination, SecretValue source, CancellationToken cancellationToken)
    {
        var versions = await vault.ListVersions(destination, cancellationToken);
        if (versions.Count == 0 || versions.Any(_ => _.HasAttributes))
        {
            return false;
        }

        foreach (var version in versions)
        {
            var value = await vault.Read(destination, version.Version, cancellationToken);
            if (value is null || !string.Equals(value.Value, source.Value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        var selected = await vault.Read(destination, null, cancellationToken);
        return selected is not null && string.Equals(selected.Value, source.Value, StringComparison.Ordinal);
    }
}
