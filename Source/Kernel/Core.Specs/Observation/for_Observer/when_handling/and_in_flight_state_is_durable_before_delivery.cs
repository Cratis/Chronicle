// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_in_flight_state_is_durable_before_delivery : given.an_observer_with_subscription_for_specific_event_type
{
    bool _wasInFlightAtDelivery;

    void Establish() => _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
        .Returns(_ =>
        {
            _wasInFlightAtDelivery = _stateStorage.State.InFlightPartitions.Contains("source");
            return Task.FromResult(ObserverSubscriberResult.Ok(42UL));
        });

    async Task Because() => await _observer.Handle("source", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);

    [Fact] void should_persist_in_flight_before_calling_the_subscriber() => _wasInFlightAtDelivery.ShouldBeTrue();
    [Fact] void should_clear_in_flight_after_delivery() => _stateStorage.State.InFlightPartitions.Contains("source").ShouldBeFalse();
}
