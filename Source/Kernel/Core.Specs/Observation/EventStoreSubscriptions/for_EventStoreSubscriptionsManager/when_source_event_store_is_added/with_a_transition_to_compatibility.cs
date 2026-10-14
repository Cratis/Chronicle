// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_source_event_store_is_added;

public class with_a_transition_to_compatibility : given.a_manager_with_pinned_generations
{
    async Task Establish() => await _manager.Add(_definition with { GenerationDelivery = EventGenerationDelivery.Compatibility });
    async Task Because() => await _manager.SourceEventStoreAdded("Admin");
    [Fact] void should_rebind_through_the_legacy_api() => _observer.Received(1).Subscribe<IEventStoreSubscriptionObserverSubscriber>(ObserverType.External, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), "Lobby", true, Arg.Any<ObserverFilters?>(), true, automatic: true);
    [Fact] void should_not_create_an_unsubscribe_gap() => _observer.DidNotReceive().Unsubscribe();
}
