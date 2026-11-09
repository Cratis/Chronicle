// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using VaultSharp;

namespace Cratis.Chronicle.Storage.Vault;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionKeyStoreUpgradeCheck"/> for HashiCorp Vault.
/// </summary>
/// <remarks>
/// Before this version writes under <see cref="VaultPathSegments.Root"/>, the check makes sure that nothing from an
/// earlier version already sits there, and records that in the <see cref="VaultPathSegments.LayoutMarker"/> secret at
/// the root of the mount. The marker is a single leaf at the root, which earlier versions never wrote, because their
/// leaves are at least four segments deep. The check needs list on <c language="csharp">metadata/</c> and create on
/// <c language="csharp">data/~layout</c>. A full inspection also reads the secrets it finds, to tell those already
/// copied to <see cref="VaultPathSegments.Root"/> from those that still need the key upgrade.
/// </remarks>
/// <param name="store">The <see cref="IVaultSecretStore"/> to talk to Vault through.</param>
internal class VaultKeyStoreUpgradeCheck(IVaultSecretStore store) : IEncryptionKeyStoreUpgradeCheck
{
    const string FormatField = "format";

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultKeyStoreUpgradeCheck"/> class over a client.
    /// </summary>
    /// <param name="vaultClient">The <see cref="IVaultClient"/> to talk to Vault through.</param>
    /// <param name="mountPointName">KV v2 mount point. Defaults to <c language="csharp">secret</c> when <see langword="null"/> or empty.</param>
    public VaultKeyStoreUpgradeCheck(IVaultClient vaultClient, string? mountPointName = null)
        : this(new VaultSecretStore(vaultClient, mountPointName))
    {
    }

    /// <inheritdoc/>
    public async Task<EncryptionKeyStoreUpgradeStatus> Inspect(bool full, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var layout = await EnsureLayout();
        if (layout.LayoutUnusable)
        {
            return layout;
        }

        var earlierRoots = (await store.List(string.Empty)).Where(VaultLegacyLayout.IsEarlierEntry).ToList();
        if (!full)
        {
            // Each earlier root is an event store with at least one entry, so this is a lower bound.
            return layout with { EarlierNameKeys = earlierRoots.Count };
        }

        long keys = 0, erasures = 0, unresolved = 0, retained = 0;
        foreach (var entry in await VaultLegacyLayout.Walk(store, earlierRoots, cancellationToken))
        {
            if (entry.IsAmbiguous)
            {
                unresolved++;
            }
            else if (await IsRetainedCopy(entry))
            {
                retained++;
            }
            else if (entry.IsErasure)
            {
                erasures++;
            }
            else
            {
                keys++;
            }
        }

        return layout with
        {
            EarlierNameKeys = keys,
            EarlierNameErasures = erasures,
            UnresolvedShapes = unresolved,
            MigratedLegacyRetained = retained,
            Complete = true
        };
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

            if (await RootIsTaken())
            {
                return Layout(verified: false, conflict: true);
            }

            // A write that loses to another node writes nothing; the marker is read again below.
            await store.WriteIfAbsent(VaultPathSegments.LayoutMarker, new Dictionary<string, string> { [FormatField] = VaultPathSegments.Root });
            return Layout(verified: await HasMarker(), conflict: false);
        }
        catch (Exception)
        {
            // Only a read that proves the layout, or a definite not found where that means empty, lets anything be written.
            // A refusal, a timeout, a server error, a network failure or any other exception proves nothing.
            return Layout(verified: false, conflict: false);
        }
    }

    /// <summary>
    /// Tells whether the paths under <see cref="VaultPathSegments.Root"/> are free to use, without writing anything.
    /// </summary>
    /// <returns>A status with the layout verified or free to record, in conflict, or unverifiable.</returns>
    public async Task<EncryptionKeyStoreUpgradeStatus> InspectLayout()
    {
        try
        {
            if (await HasMarker())
            {
                return Layout(verified: true, conflict: false);
            }

            var conflict = await RootIsTaken();
            return Layout(verified: !conflict, conflict: conflict);
        }
        catch (Exception)
        {
            return Layout(verified: false, conflict: false);
        }
    }

    static EncryptionKeyStoreUpgradeStatus Layout(bool verified, bool conflict) =>
        new(verified, conflict, 0, 0, 0, false);

    async Task<bool> IsRetainedCopy(VaultLegacyEntry entry) =>
        await store.Read(entry.Destination!) is { } current &&
        await store.Read(entry.Path) is { } legacy &&
        VaultLegacyLayout.SameContent(legacy, current);

    async Task<bool> RootIsTaken() => (await store.List(string.Empty)).Contains($"{VaultPathSegments.Root}/");

    async Task<bool> HasMarker() =>
        await store.Read(VaultPathSegments.LayoutMarker) is { } marker &&
        marker.Data.TryGetValue(FormatField, out var format) &&
        string.Equals(format, VaultPathSegments.Root, StringComparison.Ordinal);
}
