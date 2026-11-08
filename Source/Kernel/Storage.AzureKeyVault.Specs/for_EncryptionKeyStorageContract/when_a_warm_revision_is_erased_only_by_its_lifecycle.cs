// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_EncryptionKeyStorageContract;

public class when_a_warm_revision_is_erased_only_by_its_lifecycle : given.a_fenced_store
{
    CacheEncryptionKeyStorage _cache;
    EncryptionKeyAtRevision _warm;
    EncryptionKeyAtRevision _released;
    EncryptionKeyErasure _fence;

    async Task Establish()
    {
        await Storage.SaveFor(EventStore, Namespace, Identifier, Original, 999u);
        _cache = new(Storage);
        _warm = (await _cache.TryGetAtRevisionFor(EventStore, Namespace, Identifier, 999u))!;

        // Model the C3 empty-subject erasure racing a publication above the revision floor.
        var locations = SecretLocations.For(EventStore, Namespace, Identifier);
        EncryptionKeyIdentifier authorityIdentifier = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{Client.VaultUri}|{locations.Prefix}")));
        var observed = await Authority.ReadFor(EventStore, Namespace, authorityIdentifier);
        var next = observed with
        {
            Erasure = observed.Erasure with { Version = observed.Erasure.Version + 1, ErasedThrough = 1u, ErasedLifecycle = observed.Erasure.Lifecycle, NewKeyAllowed = false }
        };
        (await Authority.CompareExchangeFor(EventStore, Namespace, authorityIdentifier, observed, next)).ShouldBeTrue();
    }

    async Task Because()
    {
        _released = (await _cache.TryGetAtRevisionFor(EventStore, Namespace, Identifier, 999u))!;
        _fence = (await _cache.GetErasureForRelease(EventStore, Namespace, Identifier))!;
    }

    [Fact] void should_reuse_the_warm_revision_pair() => _released.ShouldEqual(_warm);
    [Fact] void should_preserve_the_publication_lifecycle() => _released.StagedLifecycle.ShouldEqual(1u);
    [Fact] void should_cover_the_cached_pair_at_release() => _fence.Covers(_released).ShouldBeTrue();
    [Fact] void should_not_cover_the_revision_by_its_floor() => (_released.Revision.Value > _fence.ErasedThrough.Value).ShouldBeTrue();
    [Fact] void should_not_cover_the_material_by_its_fingerprint() => _fence.ErasedKeyFingerprints.ShouldBeEmpty();
}
