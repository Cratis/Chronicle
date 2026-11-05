// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using ComplianceKey = Cratis.Chronicle.Storage.Compliance.EncryptionKey;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyStorage.when_erasing_one_of_several_similar_identifiers;

public abstract class conformance(given.KeyStoreHarness harness) : given.a_key_store(harness)
{
    ComplianceKey _alice;
    ComplianceKey _aliceWithTrailingSpace;
    object _aliceFence;
    object _lowercaseFence;

    async Task Because()
    {
        await Provision("Alice", 0);
        await Provision("alice", 1);
        await Provision("alice ", 2);
        await _harness.Storage.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "alice");
        await _harness.Storage.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "alice");
        _alice = await Read("Alice");
        _aliceWithTrailingSpace = await Read("alice ");
        _aliceFence = await _harness.Storage.GetErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "Alice");
        _lowercaseFence = await _harness.Storage.GetErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "alice");
    }

    [Fact] public void should_keep_the_key_for_Alice() => _alice.Public.ShouldEqual(KeyFor(0).Public);
    [Fact] public void should_keep_the_key_for_alice_with_trailing_space() => _aliceWithTrailingSpace.Public.ShouldEqual(KeyFor(2).Public);
    [Fact] public void should_not_fence_Alice() => _aliceFence.ShouldBeNull();
    [Fact] public void should_fence_alice() => _lowercaseFence.ShouldNotBeNull();
}
