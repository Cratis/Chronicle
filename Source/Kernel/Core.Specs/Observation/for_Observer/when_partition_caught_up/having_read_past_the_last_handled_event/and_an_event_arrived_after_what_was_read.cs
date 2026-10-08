// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_caught_up.having_read_past_the_last_handled_event;

public class and_an_event_arrived_after_what_was_read : given.an_observer_with_one_partition_being_caught_up
{
    static EventSequenceNumber _read;

    void Establish()
    {
        _read = 50UL;
        EventSequenceHasNextEvent(_read);
    }

    async Task Because() => await _observer.PartitionCaughtUp(_partition, EventSequenceNumber.Unavailable, _read);

    [Fact] void should_not_change_the_last_handled_event() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(_lastHandledEventSequenceNumber);
    [Fact] void should_start_catchup_job_after_what_was_read() => CheckStartedCatchupJob(_read);
}
