// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// Defines the operations the key migration needs from Azure Key Vault. Every method either answers or throws; none maps a failure to an empty answer.
/// </summary>
public interface ISecretVault
{
    /// <summary>
    /// Lists the live secrets, with the attributes of their latest version.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="SecretVersionInfo"/> of every live secret.</returns>
    Task<IReadOnlyList<SecretVersionInfo>> ListLive(CancellationToken cancellationToken);

    /// <summary>
    /// Lists the names of the secrets that are deleted but still recoverable.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The names.</returns>
    Task<IReadOnlySet<string>> ListDeleted(CancellationToken cancellationToken);

    /// <summary>
    /// Lists every version of a secret.
    /// </summary>
    /// <param name="name">The secret name.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The versions, empty when the secret does not exist.</returns>
    Task<IReadOnlyList<SecretVersionInfo>> ListVersions(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Reads a secret.
    /// </summary>
    /// <param name="name">The secret name.</param>
    /// <param name="version">The version, or null for the latest.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="SecretValue"/>, or null when it does not exist.</returns>
    Task<SecretValue?> Read(string name, string? version, CancellationToken cancellationToken);

    /// <summary>
    /// Writes a new version of a secret.
    /// </summary>
    /// <param name="name">The secret name.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The version written, or null when the name is held by a deleted secret that can still be recovered.</returns>
    Task<string?> Write(string name, string value, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a secret and asks for the deleted secret to be purged.
    /// </summary>
    /// <param name="name">The secret name.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>Whether the deleted secret was purged, or is still recoverable because the vault refused the purge.</returns>
    Task<SecretRemoval> Remove(string name, CancellationToken cancellationToken);
}
