// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_InMemoryEncryptionKeyStorage.when_allowing_a_new_key;

/// <summary>
/// The other half of the ruling: erasure removes the key incarnation that exists now, so a person who comes back
/// must be protectable again. This is what a later legitimate lifecycle looks like end to end.
/// </summary>
/// <remarks>
/// The authorization mints nothing by itself - the next provisioning does, and it starts above the fence rather
/// than back at revision 1, so the new key is a successor to the erased one rather than a replacement of it. What
/// stays refused is the erased key itself: the fingerprint fence outlives the authorization, which is what makes
/// "the new key cannot read the old ciphertext" a property of the store rather than a hope about key generation.
/// </remarks>
public class and_provisioning_afterwards : given.an_in_memory_key_store
{
    EncryptionKey _original;
    EncryptionKey _successor;
    EncryptionKey _result;
    EncryptionKey? _latestAfterwards;
    Exception _offeringTheErasedKeyBack;
    EncryptionKeyErasure? _erasureAfterwards;
    bool _hasTheErasedRevision;
    bool _hasTheSuccessorRevision;

    async Task Establish()
    {
        _original = KeyNamed("original");
        _successor = KeyNamed("successor");
        await Provision(_original);
        await Erase();
    }

    async Task Because()
    {
        await AllowNewKey();
        _offeringTheErasedKeyBack = await Catch.Exception(() => Provision(_original));
        _result = await Provision(_successor);
        _latestAfterwards = await Latest();
        _erasureAfterwards = await Erasure();
        _hasTheErasedRevision = await _store.HasFor(Concepts.EventStoreName.NotSet, Concepts.EventStoreNamespaceName.NotSet, _identifier, EncryptionKeyRevision.Initial);
        _hasTheSuccessorRevision = await _store.HasFor(Concepts.EventStoreName.NotSet, Concepts.EventStoreNamespaceName.NotSet, _identifier, new EncryptionKeyRevision(2u));
    }

    [Fact] void should_provision_the_successor_key() => _result.ShouldEqual(_successor);
    [Fact] void should_serve_the_successor_key() => _latestAfterwards.ShouldEqual(_successor);
    [Fact] void should_not_serve_the_erased_key() => _latestAfterwards.ShouldNotEqual(_original);
    [Fact] void should_mint_it_above_the_fence() => _hasTheSuccessorRevision.ShouldBeTrue();
    [Fact] void should_not_reuse_the_erased_revision() => _hasTheErasedRevision.ShouldBeFalse();
    [Fact] void should_still_refuse_the_erased_key_material() => _offeringTheErasedKeyBack.ShouldBeOfExactType<EncryptionKeyErased>();
    [Fact] void should_keep_the_fence_recorded() => _erasureAfterwards.ShouldNotBeNull();
    [Fact] void should_keep_fencing_the_erased_key_material() => _erasureAfterwards!.ErasedKeyFingerprints.ShouldContain(_original.Fingerprint);
}
