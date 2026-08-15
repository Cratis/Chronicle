// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_deleting_an_encryption_key;

/// <summary>
/// Stopping at the first unreachable event store would leave the key alive in a store that was never even
/// attempted, while the caller sees a failure either way - so every store is attempted and the failures are
/// reported together. An erasure that reached three stores out of four is not an erasure, and saying so is the
/// only thing that gets the fourth one erased.
/// </summary>
public class and_one_event_store_is_unavailable : given.a_pii_manager
{
    Exception _error;

    void Establish() =>
        _keyStore
            .DeleteFor(EventStore, EventStoreNamespace, Identifier)
            .ThrowsAsync(new StoreUnreachable());

    async Task Because() => _error = await Catch.Exception(() => _manager.DeleteEncryptionKeyFor(Identifier));

    [Fact] void should_still_erase_the_key_from_the_reachable_event_store() => _keyStore.Received(1).DeleteFor(OtherEventStore, EventStoreNamespace, Identifier);
    [Fact] void should_still_fence_every_event_store() => _keyStore.Received(1).RecordErasureFor(OtherEventStore, EventStoreNamespace, Identifier);
    [Fact] void should_still_evict_the_key_from_every_silo() => _cacheClient.Received(1).Evict(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_report_the_erasure_as_incomplete() => _error.ShouldBeOfExactType<EncryptionKeyErasureIncomplete>();
    [Fact] void should_carry_the_failure_from_the_unreachable_event_store() => ((EncryptionKeyErasureIncomplete)_error).Failures.ShouldContainOnly(_error.InnerException);
}
