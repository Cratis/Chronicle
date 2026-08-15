// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_deleting_an_encryption_key;

/// <summary>
/// The erasure needs the list of event stores to know how far to reach. Losing the list must not lose the erasure:
/// the event store the caller named is erased either way, and the incomplete reach is reported rather than left to
/// look like a completed erasure.
/// </summary>
public class and_the_event_stores_cannot_be_listed : given.a_pii_manager
{
    Exception _error;

    void Establish() => _storage.GetEventStores().ThrowsAsync(new StoreUnreachable());

    async Task Because() => _error = await Catch.Exception(() => _manager.DeleteEncryptionKeyFor(Identifier));

    [Fact] void should_still_fence_the_event_store_it_was_asked_for() => _keyStore.Received(1).RecordErasureFor(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_still_erase_the_key_from_it() => _keyStore.Received(1).DeleteFor(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_still_evict_the_key_from_every_silo() => _cacheClient.Received(1).Evict(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_not_reach_an_event_store_it_could_not_learn_about() => _keyStore.DidNotReceive().DeleteFor(OtherEventStore, EventStoreNamespace, Identifier);
    [Fact] void should_report_the_erasure_as_incomplete() => _error.ShouldBeOfExactType<EncryptionKeyErasureIncomplete>();
}
