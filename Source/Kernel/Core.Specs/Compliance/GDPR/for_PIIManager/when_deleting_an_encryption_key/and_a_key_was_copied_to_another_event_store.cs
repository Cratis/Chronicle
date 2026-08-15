// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_deleting_an_encryption_key;

/// <summary>
/// The reported defect in one spec: a cross-event-store subscription copies a subject's key into the event store it
/// forwards into, and an erasure that reached only the event store it was addressed at left that copy readable -
/// and a later forwarded event copied the survivor back into the store that had been cleared.
/// </summary>
/// <remarks>
/// The manager is addressed at one event store and the namespace holds two. Both are fenced and both are cleared,
/// and every fence is recorded before any key material is destroyed, so there is no moment at which one store has
/// lost its key while another would still hand a copy over.
/// </remarks>
public class and_a_key_was_copied_to_another_event_store : given.a_pii_manager
{
    Task Because() => _manager.DeleteEncryptionKeyFor(Identifier);

    [Fact] void should_fence_the_event_store_it_was_asked_for() => _keyStore.Received(1).RecordErasureFor(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_fence_the_event_store_the_key_was_copied_into() => _keyStore.Received(1).RecordErasureFor(OtherEventStore, EventStoreNamespace, Identifier);
    [Fact] void should_erase_the_key_from_the_event_store_it_was_asked_for() => _keyStore.Received(1).DeleteFor(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_erase_the_copy_from_the_other_event_store() => _keyStore.Received(1).DeleteFor(OtherEventStore, EventStoreNamespace, Identifier);
    [Fact] void should_evict_the_copy_from_every_silo() => _cacheClient.Received(1).Evict(OtherEventStore, EventStoreNamespace, Identifier);
    [Fact] void should_fence_every_event_store_before_destroying_any_key() => Received.InOrder(() =>
    {
        _keyStore.RecordErasureFor(EventStore, EventStoreNamespace, Identifier);
        _keyStore.RecordErasureFor(OtherEventStore, EventStoreNamespace, Identifier);
        _keyStore.DeleteFor(EventStore, EventStoreNamespace, Identifier);
        _keyStore.DeleteFor(OtherEventStore, EventStoreNamespace, Identifier);
    });
}
