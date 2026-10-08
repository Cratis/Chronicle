// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing_to_all_events;

public class and_storage_has_no_record : given.an_observer
{
    public and_storage_has_no_record()
    {
        _silo.Options.StorageFactory = type => type == typeof(ObserverState)
            ? new given.provider_backed_observer_state_storage(new ObserverStateGrainStorageProvider(_storage), _observerKey)
            : null!;
    }

    void Establish()
    {
        // The activation already knows its identity, but the next read through the real provider finds no record.
        _eventStoreNamespaceStorage.Observers.Get(_observerId).Returns(ObserverState.Empty);
        _eventStoreNamespaceStorage.Observers.ClearReceivedCalls();
        _appendedEventsQueues.ClearReceivedCalls();
    }

    async Task Because() => await _observer.SubscribeToAllEvents<ObserverSubscriber>(ObserverType.Projection, SiloAddress.Zero);

    [Fact] async Task should_keep_the_activation_identity() => (await _observer.GetState()).Identifier.ShouldEqual(_observerId);
    [Fact] void should_persist_under_its_own_identity() => _eventStoreNamespaceStorage.Observers.Received().Save(Arg.Is<ObserverState>(state => state.Identifier == _observerId && state.SubscribesToAllEvents));
    [Fact] void should_never_persist_under_the_unspecified_identity() => _eventStoreNamespaceStorage.Observers.DidNotReceive().Save(Arg.Is<ObserverState>(state => state.Identifier == ObserverId.Unspecified));
    [Fact] void should_subscribe_to_the_queue_with_its_own_key() => _appendedEventsQueues.Received(1).SubscribeToAllEventTypes(_observerKey, Arg.Any<ObserverFilters?>());
    [Fact] async Task should_be_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
}
