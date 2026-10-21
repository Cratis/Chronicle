// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_starting_at_tail;

public class and_the_event_sequence_is_empty : given.an_absent_observer
{
    bool _result;

    void Establish()
    {
        _stateStorage.State = ObserverState.Empty;
        _storageStats.ResetCounts();
    }

    async Task Because() => _result = await _observer.StartAtTailWhenNew();

    [Fact] void should_report_that_it_was_positioned() => _result.ShouldBeTrue();
    [Fact] void should_expect_the_first_event() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_actually_write_the_state() => _storageStats.Writes.ShouldBeGreaterThan(0);
    [Fact] void should_persist_the_identifier_so_a_later_registration_is_not_mistaken_for_the_first() => _stateStorage.State.Identifier.ShouldEqual(_observerId);
}
