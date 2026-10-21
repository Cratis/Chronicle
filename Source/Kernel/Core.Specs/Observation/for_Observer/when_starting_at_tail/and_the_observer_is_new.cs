// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_starting_at_tail;

public class and_the_observer_is_new : given.an_absent_observer
{
    bool _result;

    void Establish()
    {
        // Nothing has ever been persisted for this observer.
        _stateStorage.State = ObserverState.Empty;
        _storageStats.ResetCounts();
        _eventSequence.GetTailSequenceNumber().Returns(Task.FromResult<EventSequenceNumber>(100UL));
    }

    async Task Because() => _result = await _observer.StartAtTailWhenNew();

    [Fact] void should_report_that_it_was_positioned() => _result.ShouldBeTrue();
    [Fact] void should_expect_the_event_after_the_tail() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual<EventSequenceNumber>(101UL);
    [Fact] void should_record_the_tail() => _stateStorage.State.TailEventSequenceNumber.ShouldEqual<EventSequenceNumber>(100UL);
    [Fact] void should_actually_write_the_state() => _storageStats.Writes.ShouldBeGreaterThan(0);
    [Fact] void should_persist_the_identifier_so_it_is_no_longer_new() => _stateStorage.State.Identifier.ShouldEqual(_observerId);
}
