// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_deleting_an_encryption_key;

public class and_one_fence_is_unavailable : given.a_pii_manager
{
    Exception _error;

    void Establish() =>
        _keyStore
            .RecordErasureFor(EventStore, EventStoreNamespace, Identifier, Arg.Any<EncryptionKeyErasureRequest>())
            .ThrowsAsync(new EncryptionKeyFenceUnavailable(Identifier));

    async Task Because() => _error = await Catch.Exception(() => _manager.DeleteEncryptionKeyFor(Identifier));

    [Fact] void should_try_the_unavailable_fence_only_twice() => _keyStore.Received(2).RecordErasureFor(EventStore, EventStoreNamespace, Identifier, Arg.Any<EncryptionKeyErasureRequest>());
    [Fact] void should_not_sweep_the_unavailable_store() => _keyStore.DidNotReceive().DeleteThroughFor(EventStore, EventStoreNamespace, Identifier, Arg.Any<string>());
    [Fact] void should_still_sweep_the_reachable_store() => _keyStore.Received(1).DeleteThroughFor(OtherEventStore, EventStoreNamespace, Identifier, Arg.Any<string>());
    [Fact] void should_still_evict_the_key_from_every_silo() => _cacheClient.Received(1).Evict(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_never_confirm() => _keyStore.DidNotReceive().ConfirmErasureFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Identifier, Arg.Any<string>());
    [Fact] void should_report_the_erasure_as_incomplete() => _error.ShouldBeOfExactType<EncryptionKeyErasureIncomplete>();
}
