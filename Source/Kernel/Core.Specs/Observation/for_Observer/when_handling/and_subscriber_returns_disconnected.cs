// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_subscriber_returns_disconnected : given.an_observer_with_subscription_for_specific_event_type
{
    void Establish()
    {
        _subscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(Task.FromResult(ObserverSubscriberResult.Disconnected()));
        _stateStorage.State = _stateStorage.State with
        {
            NextEventSequenceNumber = 42UL,
            LastHandledEventSequenceNumber = 41UL
        };
    }

    async Task Because() => await _observer.Handle("Something", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);

    [Fact] void should_write_state_once() => _storageStats.Writes.ShouldEqual(1);
    [Fact] void should_set_running_state_to_disconnected() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Disconnected);
    [Fact] void should_not_change_next_sequence_number() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_not_change_last_handled_event_sequence_number() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)41UL);
    [Fact] async Task should_set_subscription_to_unsubscribed() => (await _observer.GetSubscription()).ShouldEqual(ObserverSubscription.Unsubscribed);
}
