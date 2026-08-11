// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

public class when_deleting_during_an_in_flight_provisioning : given.a_cache_encryption_key_store
{
    static readonly EncryptionKeyIdentifier _identifier = "0f0a1b70-2a94-4e35-9a4e-6d6c0b8f1a21";
    static readonly EncryptionKey _key = new([1], [2]);

    readonly TaskCompletionSource _provisioningStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseProvisioning = new(TaskCreationOptions.RunContinuationsAsynchronously);
    EncryptionKey? _afterDeletion;

    void Establish() =>
        _actualStore
            .GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _key)
            .Returns(_ => ProvisionBlockedKey());

    async Task Because()
    {
        var inFlightProvisioning = _store.GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _key);
        await _provisioningStarted.Task;
        await _store.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        _releaseProvisioning.SetResult();
        await inFlightProvisioning;

        _actualStore
            .TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier)
            .Returns(Task.FromResult<EncryptionKey?>(null));
        _afterDeletion = await _store.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
    }

    async Task<EncryptionKey> ProvisionBlockedKey()
    {
        _provisioningStarted.SetResult();
        await _releaseProvisioning.Task;
        return _key;
    }

    [Fact] void should_not_restore_the_deleted_key_to_the_cache() => _afterDeletion.ShouldBeNull();
    [Fact] void should_read_the_backing_store_for_the_key() => _actualStore.Received(1).TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
}
