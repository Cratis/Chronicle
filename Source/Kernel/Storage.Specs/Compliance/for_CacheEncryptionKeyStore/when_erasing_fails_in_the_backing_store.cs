// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

/// <summary>
/// A backing store that reports a failed erase has not necessarily erased nothing - the composite store attempts every
/// inner store and then reports a partial failure, and a store that erases revision by revision can throw part way
/// through. The cache therefore cannot treat a failed erase as "nothing happened" and keep serving what it holds; the
/// invalidation that follows the erase has to run whether the erase succeeded or threw.
/// </summary>
public class when_erasing_fails_in_the_backing_store : given.a_cache_encryption_key_store
{
    static readonly EncryptionKeyIdentifier _identifier = "c8b04f2a-9e6d-41b8-b3a7-5d0e2f7c1a48";
    static readonly EncryptionKey _key = new([1], [2]);

    readonly TaskCompletionSource _erasureStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseErasure = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception _error;
    EncryptionKey? _duringErasure;
    EncryptionKey? _afterFailedErasure;

    void Establish()
    {
        _actualStore
            .DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier)
            .Returns(_ => FailOnceReleased());

        _actualStore
            .TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier)
            .Returns(Task.FromResult<EncryptionKey?>(_key));
    }

    async Task Because()
    {
        var inFlightErasure = _store.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        await _erasureStarted.Task;

        _duringErasure = await _store.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);

        _releaseErasure.SetResult();
        _error = await Catch.Exception(() => inFlightErasure);

        _actualStore
            .TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier)
            .Returns(Task.FromResult<EncryptionKey?>(null));
        _afterFailedErasure = await _store.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
    }

    async Task FailOnceReleased()
    {
        _erasureStarted.SetResult();
        await _releaseErasure.Task;
        throw new ErasureFailed();
    }

    [Fact] void should_report_the_failure_to_the_caller() => _error.ShouldBeOfExactType<ErasureFailed>();
    [Fact] void should_hand_the_racing_read_what_the_backing_store_still_held() => _duringErasure.ShouldEqual(_key);
    [Fact] void should_not_keep_serving_what_may_already_be_gone() => _afterFailedErasure.ShouldBeNull();
    [Fact] void should_read_the_backing_store_again() => _actualStore.Received(2).TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
}
