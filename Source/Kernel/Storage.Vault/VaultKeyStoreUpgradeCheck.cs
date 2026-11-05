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

        var earlierRoots = (await ListRootEntries()).Where(IsEarlierEntry).ToList();
        if (!full)
        {
            // Each earlier root is an event store with at least one entry, so this is a lower bound.
            return layout with { EarlierNameKeys = earlierRoots.Count };
        }

        var counts = new Counts();
        foreach (var root in earlierRoots)
        {
            await Walk([root.TrimEnd('/')], counts, cancellationToken);
        }

        return layout with { EarlierNameKeys = counts.Keys, EarlierNameErasures = counts.Erasures, UnresolvedShapes = counts.Unresolved, Complete = true };
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
        catch (Exception)
        {
            // Only a read that proves the layout, or a definite not found where that means empty, lets anything be written.
            // A refusal, a timeout, a server error, a network failure or any other exception proves nothing.
            return Layout(verified: false, conflict: false);
        }
    }

    static EncryptionKeyStoreUpgradeStatus Layout(bool verified, bool conflict) =>
        new(verified, conflict, 0, 0, 0, false);

    /// <summary>
    /// Gets whether a root entry is an event store from an earlier version. Only the two names this version reserves are
    /// skipped, and only when they are exactly those: the root directory and the marker leaf. Anything else, whatever
    /// character it starts with, is an event store from an earlier version.
    /// </summary>
    /// <param name="entry">The entry listed at the root of the mount.</param>
    /// <returns>True when the entry is from an earlier version, false otherwise.</returns>
    static bool IsEarlierEntry(string entry) => entry != $"{VaultPathSegments.Root}/" && entry != VaultPathSegments.LayoutMarker;

    static void Classify(List<string> segments, Counts counts)
    {
        var eligible = segments.Count == 4 &&
            VaultPathSegments.LegacyDirectory(segments[0], segments[1], segments[2]) is not null;
        if (eligible && segments[3] == "erasure")
        {
            counts.Erasures++;
        }
        else if (eligible && uint.TryParse(segments[3], out _))
        {
            counts.Keys++;
        }
        else
        {
            counts.Unresolved++;
        }
    }

    async Task Walk(List<string> path, Counts counts, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entries = await ListEntries(string.Join('/', path));
        foreach (var entry in entries)
        {
            var segments = new List<string>(path) { entry.TrimEnd('/') };
            if (entry.EndsWith('/'))
            {
                await Walk(segments, counts, cancellationToken);
            }
            else
            {
                Classify(segments, counts);
            }
        }
    }

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

    async Task<List<string>> ListRootEntries() => await ListEntries(string.Empty);

    async Task<List<string>> ListEntries(string path)
    {
        try
        {
            var list = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretPathsAsync(path, mountPoint: _mountPoint);
            return [.. list.Data.Keys];
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }
    }

    sealed class Counts
    {
        public long Keys { get; set; }

        public long Erasures { get; set; }

        public long Unresolved { get; set; }
    }
}
