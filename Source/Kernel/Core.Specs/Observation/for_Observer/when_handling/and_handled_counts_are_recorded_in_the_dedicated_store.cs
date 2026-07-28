// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_handled_counts_are_recorded_in_the_dedicated_store : given.an_observer_with_subscription_for_specific_event_type
{
    void Establish() =>
          _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(ObserverSubscriberResult.Ok(42UL));

    async Task Because() => await _observer.Handle("Something", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);

    [Fact]
    void should_increment_the_dedicated_store_with_the_handled_counts() => _observerHandledCountsStorage.Received(1).Increment(
        _observerId,
        Arg.Is<Key>(_ => _.IsEqualTo("Something")),
        Arg.Is<IReadOnlyDictionary<EventTypeId, EventCount>>(_ => _.Count == 1 && _[event_type.Id] == (EventCount)1UL));

    [Fact]
    void should_keep_the_running_total_on_the_observer_state() => _stateStorage.State.HandledEventCount.ShouldEqual((EventCount)1UL);

    [Fact]
    void should_keep_the_running_per_event_type_total_on_the_observer_state() => _stateStorage.State.HandledEventCountPerEventType[event_type.Id].ShouldEqual((EventCount)1UL);
}
