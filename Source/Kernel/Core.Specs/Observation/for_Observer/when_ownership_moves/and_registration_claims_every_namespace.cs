// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Observation.for_Observer.when_ownership_moves;

public class and_registration_claims_every_namespace : given.a_live_observer_with_guarded_state
{
    readonly Storage.InMemory.Observation.ObserverStateStorage _other = new();
    readonly Storage.InMemory.Observation.ObserverStateStorage _empty = new();

    async Task Establish()
    {
        _storedDefinition = _storedDefinition with { EventSequenceId = EventSequenceId.Inbox };
        _definitionStorage.State = _storedDefinition;
        await _guarded.ClaimOwnership(_observerId, EventSequenceId.Inbox);
        await _other.Save(new() { Identifier = _observerId, LastHandledEventSequenceNumber = 42 }, EventSequenceId.Inbox);
        var otherNamespace = Substitute.For<Storage.IEventStoreNamespaceStorage>();
        otherNamespace.Observers.Returns(_other);
        var emptyNamespace = Substitute.For<Storage.IEventStoreNamespaceStorage>();
        emptyNamespace.Observers.Returns(_empty);
        _eventStoreStorage.GetNamespace("other").Returns(otherNamespace);
        _eventStoreStorage.GetNamespace("empty").Returns(emptyNamespace);
        _eventStoreStorage.Namespaces.GetAll().Returns([
            new NamespaceState(_observerKey.Namespace, DateTimeOffset.UnixEpoch),
            new NamespaceState("other", DateTimeOffset.UnixEpoch),
            new NamespaceState("empty", DateTimeOffset.UnixEpoch)
        ]);
    }

    async Task Because() => await _observer.SubscribeToAllEvents<NullObserverSubscriber>(ObserverType.Projection, SiloAddress.Zero);

    [Fact] async Task should_claim_the_current_namespace() => (await _guarded.Get(_observerId)).Owner.ShouldEqual(EventSequenceId.Log);
    [Fact] async Task should_claim_the_other_namespace() => (await _other.Get(_observerId)).Owner.ShouldEqual(EventSequenceId.Log);
    [Fact] async Task should_preserve_the_other_namespaces_progress() => (await _other.Get(_observerId)).LastHandledEventSequenceNumber.ShouldEqual((Concepts.Events.EventSequenceNumber)42);
    [Fact] async Task should_not_create_state_in_the_empty_namespace() => (await _empty.GetAll()).ShouldBeEmpty();

    void Destroy()
    {
        _other.Dispose();
        _empty.Dispose();
    }
}
