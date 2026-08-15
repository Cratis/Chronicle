// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_allowing_a_new_encryption_key;

/// <summary>
/// Erasure removes the key incarnation that exists now; it is not a permanent ban on the subject identifier. This
/// is the deliberate step that lets the same person be protected again, and it has to cover the same set of event
/// stores the erasure did - a subject fenced in one event store and open in another cannot have their events
/// forwarded between the two.
/// </summary>
/// <remarks>
/// It creates no key. A silo that remembers the subject as absent would answer from that memory rather than reach
/// the store where the authorization now sits, so the eviction is part of the operation rather than a nicety.
/// </remarks>
public class and_the_subject_was_erased : given.a_pii_manager
{
    Task Because() => _manager.AllowNewEncryptionKeyFor(Identifier);

    [Fact] void should_authorize_a_new_key_in_the_event_store_it_was_asked_for() => _keyStore.Received(1).AllowNewKeyFor(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_authorize_a_new_key_in_every_other_event_store_in_the_namespace() => _keyStore.Received(1).AllowNewKeyFor(OtherEventStore, EventStoreNamespace, Identifier);
    [Fact] void should_not_provision_a_key_itself() => _keyStore.DidNotReceive().SaveFor(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<Storage.Compliance.EncryptionKey>(), Arg.Any<EncryptionKeyRevision?>());
    [Fact] void should_evict_the_remembered_absence_from_every_silo() => _cacheClient.Received(1).Evict(EventStore, EventStoreNamespace, Identifier);
}
