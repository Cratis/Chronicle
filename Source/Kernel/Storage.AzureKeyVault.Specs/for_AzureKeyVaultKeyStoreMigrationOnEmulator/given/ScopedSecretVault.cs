// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrationOnEmulator.given;

/// <summary>
/// Wraps the real <see cref="SecretClientVault"/> so that listings only show secrets of one event store, as the emulator is shared by every spec.
/// </summary>
/// <param name="inner">The <see cref="ISecretVault"/> to delegate to.</param>
/// <param name="prefix">The prefix of the secret names that are visible when listing.</param>
public class ScopedSecretVault(ISecretVault inner, string prefix) : ISecretVault
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<SecretVersionInfo>> ListLive(CancellationToken cancellationToken) =>
        [.. (await inner.ListLive(cancellationToken)).Where(_ => _.Name.StartsWith(prefix, StringComparison.Ordinal))];

    /// <inheritdoc/>
    public async Task<IReadOnlySet<string>> ListDeleted(CancellationToken cancellationToken) =>
        new HashSet<string>((await inner.ListDeleted(cancellationToken)).Where(_ => _.StartsWith(prefix, StringComparison.Ordinal)), StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public Task<IReadOnlyList<SecretVersionInfo>> ListVersions(string name, CancellationToken cancellationToken) => inner.ListVersions(name, cancellationToken);

    /// <inheritdoc/>
    public Task<SecretValue?> Read(string name, string? version, CancellationToken cancellationToken) => inner.Read(name, version, cancellationToken);

    /// <inheritdoc/>
    public Task<string?> Write(string name, string value, CancellationToken cancellationToken) => inner.Write(name, value, cancellationToken);

    /// <inheritdoc/>
    public Task<SecretRemoval> Remove(string name, CancellationToken cancellationToken) => inner.Remove(name, cancellationToken);
}
