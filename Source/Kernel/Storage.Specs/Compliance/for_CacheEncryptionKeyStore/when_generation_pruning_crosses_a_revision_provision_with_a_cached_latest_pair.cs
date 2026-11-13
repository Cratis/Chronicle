// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

public class when_generation_pruning_crosses_a_revision_provision_with_a_cached_latest_pair : given.a_cache_with_generation_pruning
{
    readonly TaskCompletionSource<EncryptionKeyAtRevision> _provisioned = new(TaskCreationOptions.RunContinuationsAsynchronously);
    EncryptionKeyAtRevision _newPair;
    EncryptionKeyAtRevision _result;

    void Establish()
    {
        _newPair = new(_newKey, 2);
        _actualStore.GetOrAddWithRevisionFor("store", "ns", "subject", _newKey).Returns(_provisioned.Task);
        _actualStore.TryGetLatestFor("store", "ns", "subject").Returns(new EncryptionKeyAtRevision(_oldKey, EncryptionKeyRevision.Initial));
    }

    async Task Because()
    {
        var provision = _store.GetOrAddWithRevisionFor("store", "ns", "subject", _newKey);
        await _store.TryGetLatestFor("store", "ns", "subject");
        PruneGenerations();
        _actualStore.TryGetLatestFor("store", "ns", "subject").Returns(_newPair);
        _provisioned.SetResult(_newPair);
        _result = await provision;
    }

    [Fact] void should_return_the_new_pair_instead_of_the_cached_pair() => _result.ShouldEqual(_newPair);
    [Fact] void should_read_storage_again() => _actualStore.Received(2).TryGetLatestFor("store", "ns", "subject");
}
