// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.for_Observer.when_reconciling_subscription;

public class and_all_events_include_future_types : given.an_observer
{
    readonly EventType _future = new("future-event", EventTypeGeneration.First);
    IEnumerable<EventType> _types;

    async Task Because()
    {
        await _observer.ReconcileSubscription<NullObserverSubscriber>(ObserverType.Projection, [EventType.Unknown], SiloAddress.Zero, true);
        _eventTypesStorage.GetLatestForAllEventTypes().Returns([new EventTypeSchema(_future, EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema())]);
        _types = await _observer.GetEventTypes();
    }

    [Fact] async Task should_remain_a_wildcard() => (await _observer.GetState()).SubscribesToAllEvents.ShouldBeTrue();
    [Fact] async Task should_have_no_fixed_subscription_types() => (await _observer.GetSubscription()).EventTypes.ShouldBeEmpty();
    [Fact] void should_have_no_fixed_definition_types() => _definitionStorage.State.EventTypes.ShouldBeEmpty();
    [Fact] void should_include_types_registered_after_subscription() => _types.ShouldContainOnly(_future);
}
