// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

public class when_generation_pruning_crosses_a_provision_with_cached_absence : given.a_cache_with_generation_pruning
{
    readonly TaskCompletionSource<EncryptionKey> _provisioned = new(TaskCreationOptions.RunContinuationsAsynchronously);
    EncryptionKey _result;

    void Establish() => _actualStore.GetOrAddFor("store", "ns", "subject", _newKey).Returns(_provisioned.Task);

    async Task Because()
    {
        var provision = _store.GetOrAddFor("store", "ns", "subject", _newKey);
        await _store.TryGetFor("store", "ns", "subject");
        PruneGenerations();
        _actualStore.TryGetFor("store", "ns", "subject").Returns(_newKey);
        _provisioned.SetResult(_newKey);
        _result = await provision;
    }

    [Fact] void should_return_the_provisioned_key_instead_of_the_cached_absence() => _result.ShouldEqual(_newKey);
    [Fact] void should_read_storage_again() => _actualStore.Received(2).TryGetFor("store", "ns", "subject");
}
