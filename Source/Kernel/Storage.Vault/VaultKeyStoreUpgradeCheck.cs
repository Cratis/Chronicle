// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Storage.Compliance;
using VaultSharp;
using VaultSharp.Core;

namespace Cratis.Chronicle.Storage.Vault;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionKeyStoreUpgradeCheck"/> for HashiCorp Vault.
/// </summary>
/// <remarks>
/// Before this version writes under <see cref="VaultPathSegments.Root"/>, the check makes sure that nothing from an
/// earlier version already sits there, and records that in the <see cref="VaultPathSegments.LayoutMarker"/> secret at
/// the root of the mount. The marker is a single leaf at the root, which earlier versions never wrote, because their
/// leaves are at least four segments deep. The check needs list on <c language="csharp">metadata/</c> and create on
/// <c language="csharp">data/~layout</c>.
/// </remarks>
/// <param name="vaultClient">The <see cref="IVaultClient"/> to talk to Vault through.</param>
/// <param name="mountPointName">KV v2 mount point. Defaults to <c language="csharp">secret</c> when <see langword="null"/> or empty.</param>
internal class VaultKeyStoreUpgradeCheck(IVaultClient vaultClient, string? mountPointName = null) : IEncryptionKeyStoreUpgradeCheck
{
    const string FormatField = "format";
    const string DefaultMountPoint = "secret";

    readonly IVaultClient _vaultClient = vaultClient;
    readonly string _mountPoint = string.IsNullOrEmpty(mountPointName) ? DefaultMountPoint : mountPointName;

    /// <inheritdoc/>
    public async Task<EncryptionKeyStoreUpgradeStatus> Inspect(bool full, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var layout = await EnsureLayout();
        if (layout.LayoutUnusable)
        {
            return layout;
        }

        var earlier = await ListRootEntries();
        return layout with { EarlierNameKeys = earlier.Count(IsEarlierEntry) };
    }

    /// <summary>
    /// Makes sure the paths under <see cref="VaultPathSegments.Root"/> are free to use, and records that.
    /// </summary>
    /// <returns>A status with the layout verified, in conflict, or unverifiable.</returns>
    public async Task<EncryptionKeyStoreUpgradeStatus> EnsureLayout()
    {
        try
        {
            if (await HasMarker())
            {
                return Layout(verified: true, conflict: false);
            }

            if ((await ListRootEntries()).Contains($"{VaultPathSegments.Root}/"))
            {
                return Layout(verified: false, conflict: true);
            }

            try
            {
                await _vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(
                    VaultPathSegments.LayoutMarker,
                    new Dictionary<string, object> { [FormatField] = VaultPathSegments.Root },
                    checkAndSet: 0,
                    mountPoint: _mountPoint);
            }
            catch (VaultApiException ex) when (VaultEncryptionKeyStorage.IsCheckAndSetMismatch(ex))
            {
                // Another node wrote the marker first; read it again below.
            }

            return Layout(verified: await HasMarker(), conflict: false);
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == HttpStatusCode.Forbidden)
        {
            return Layout(verified: false, conflict: false);
        }
    }

    static EncryptionKeyStoreUpgradeStatus Layout(bool verified, bool conflict) =>
        new(verified, conflict, 0, 0, 0, false);

    static bool IsEarlierEntry(string entry) => entry != $"{VaultPathSegments.Root}/" && !entry.StartsWith('~');

    async Task<bool> HasMarker()
    {
        try
        {
            var secret = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(VaultPathSegments.LayoutMarker, mountPoint: _mountPoint);
            return secret.Data.Data.TryGetValue(FormatField, out var format) &&
                string.Equals(format?.ToString(), VaultPathSegments.Root, StringComparison.Ordinal);
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    async Task<List<string>> ListRootEntries()
    {
        try
        {
            var list = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretPathsAsync(string.Empty, mountPoint: _mountPoint);
            return [.. list.Data.Keys];
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }
    }
}
