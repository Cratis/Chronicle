// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Grains.Observation.for_Observer.when_handling;

public class and_subscriber_returns_disconnected : given.an_observer_with_subscription_for_specific_event_type
{
    void Establish()
    {
        subscriber
            .Setup(_ => _.OnNext(IsAny<IEnumerable<AppendedEvent>>(), IsAny<ObserverSubscriberContext>()))
            .Returns(Task.FromResult(ObserverSubscriberResult.Disconnected()));
        state_storage.State = state_storage.State with
        {
            NextEventSequenceNumber = 42UL,
            LastHandledEventSequenceNumber = 41UL
        };
    }

    async Task Because() => await observer.Handle("Something", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);

    [Fact] void should_write_state_once() => storage_stats.Writes.ShouldEqual(1);
    [Fact] void should_set_running_state_to_disconnected() => state_storage.State.RunningState.ShouldEqual(ObserverRunningState.Disconnected);
    [Fact] void should_not_change_next_sequence_number() => state_storage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_not_change_last_handled_event_sequence_number() => state_storage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)41UL);
    [Fact] async Task should_set_subscription_to_unsubscribed() => (await observer.GetSubscription()).ShouldEqual(ObserverSubscription.Unsubscribed);
}
