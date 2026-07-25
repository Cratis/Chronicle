// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_debouncing_progress_persistence;

public class and_the_batch_interval_is_not_reached : given.an_observer_debouncing_progress_persistence
{
    async Task Because()
    {
        await _observer.Handle("p1", [AppendedEvent.EmptyWithEventSequenceNumber(0UL)]);
        await _observer.Handle("p1", [AppendedEvent.EmptyWithEventSequenceNumber(1UL)]);
    }

    [Fact] void should_not_persist_progress_before_the_interval_is_reached() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_still_advance_next_event_sequence_number_in_memory() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)2UL);
}
