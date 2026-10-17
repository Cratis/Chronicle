// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_reconciling_subscription;

public class and_older_generation_pins_replace_membership : given.an_observer
{
    readonly EventType _pin = new("some-event", EventTypeGeneration.First);
    ObserverSubscription _subscription;

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Projection, [new EventType(_pin.Id, 2), EventType.Unknown], SiloAddress.Zero);
        _eventTypesStorage.HasFor(_pin.Id, _pin.Generation).Returns(true);
        _storageStats.ResetCounts();
    }

    async Task Because()
    {
        await _observer.ReconcileSubscription<NullObserverSubscriber>(ObserverType.Projection, [_pin], SiloAddress.Zero, false, EventGenerationDelivery.Pinned);
        _subscription = await _observer.GetSubscription();
    }

    [Fact] void should_replace_rather_than_union_membership() => _subscription.EventTypes.ShouldContainOnly(_pin);
    [Fact] void should_preserve_the_delivery_policy() => _subscription.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_persist_the_exact_pin() => _definitionStorage.State.EventTypes.ShouldContainOnly(_pin);
    [Fact] void should_persist_the_delivery_policy() => _definitionStorage.State.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_not_reload_progress() => _storageStats.Reads.ShouldEqual(0);
}
