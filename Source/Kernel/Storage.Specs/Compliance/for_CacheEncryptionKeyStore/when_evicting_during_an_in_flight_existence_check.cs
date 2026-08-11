// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

public class when_evicting_during_an_in_flight_existence_check : given.a_cache_encryption_key_store
{
    static readonly EncryptionKeyIdentifier _identifier = "b3f1c0a8-5d2e-4c77-8a10-9f4b2e7d6c33";

    readonly TaskCompletionSource _checkStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseCheck = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _afterEviction;

    void Establish() =>
        _actualStore
            .HasFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier)
            .Returns(_ => CheckBlockedAbsence());

    async Task Because()
    {
        var inFlightCheck = _store.HasFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        await _checkStarted.Task;
        _store.EvictFromCache(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        _releaseCheck.SetResult();
        await inFlightCheck;

        _actualStore
            .HasFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier)
            .Returns(Task.FromResult(true));
        _afterEviction = await _store.HasFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
    }

    async Task<bool> CheckBlockedAbsence()
    {
        _checkStarted.SetResult();
        await _releaseCheck.Task;
        return false;
    }

    [Fact] void should_not_remember_the_absence_the_eviction_invalidated() => _afterEviction.ShouldBeTrue();
    [Fact] void should_read_the_backing_store_again() => _actualStore.Received(2).HasFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
}
