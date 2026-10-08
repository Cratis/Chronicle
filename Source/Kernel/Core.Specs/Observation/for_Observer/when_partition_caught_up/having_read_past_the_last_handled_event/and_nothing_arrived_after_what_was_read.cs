// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_caught_up.having_read_past_the_last_handled_event;

/// <summary>
/// The events after the last handled one were read and excluded by the observer's filters. They must not start
/// another catch-up that reads them again, and they are not counted as handled.
/// </summary>
public class and_nothing_arrived_after_what_was_read : given.an_observer_with_one_partition_being_caught_up
{
    static EventSequenceNumber _handled;
    static EventSequenceNumber _read;

    void Establish()
    {
        _handled = _lastHandledEventSequenceNumber.Next();
        _read = 50UL;
        EventSequenceHasNextEvent(_handled);
        EventSequenceDoesNotHaveNextEvent(_read);
    }

    async Task Because() => await _observer.PartitionCaughtUp(_partition, _handled, _read);

    [Fact] void should_remove_partition_from_catching_up_partitions() => _stateStorage.State.CatchingUpPartitions.ShouldNotContain(_partition);
    [Fact] void should_count_only_the_handled_event_as_handled() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(_handled);
    [Fact] void should_not_start_catchup_job() => CheckDidNotStartCatchupJob();
}
