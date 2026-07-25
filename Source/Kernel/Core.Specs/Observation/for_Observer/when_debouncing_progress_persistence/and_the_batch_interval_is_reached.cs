// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_debouncing_progress_persistence;

public class and_the_batch_interval_is_reached : given.an_observer_debouncing_progress_persistence
{
    async Task Because()
    {
        // Each batch holds only events the observer is not subscribed to, so every batch advances
        // NextEventSequenceNumber but writes nothing until the debounce interval is reached.
        await _observer.Handle("p1", [AppendedEvent.EmptyWithEventSequenceNumber(0UL)]);
        await _observer.Handle("p1", [AppendedEvent.EmptyWithEventSequenceNumber(1UL)]);
        await _observer.Handle("p1", [AppendedEvent.EmptyWithEventSequenceNumber(2UL)]);
    }

    [Fact] void should_persist_state_only_once_for_the_whole_interval() => _storageStats.Writes.ShouldEqual(1);
    [Fact] void should_advance_next_event_sequence_number_past_the_skipped_events() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_not_forward_any_event_to_the_subscriber() => _subscriber.DidNotReceive().OnNext(Arg.Any<Concepts.Keys.Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
}
