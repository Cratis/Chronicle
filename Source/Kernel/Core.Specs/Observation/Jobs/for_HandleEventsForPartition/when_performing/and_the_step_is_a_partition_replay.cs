// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_the_step_is_a_partition_replay : given.a_performing_job_step_with_observer_filters
{
    void Establish()
    {
        _performState.EventObservationState = EventObservationState.Replay;
        _eventCursor.Current.Returns([CreateEvent(1UL, _audited)]);
        _observer.IsPartitionFenced(Arg.Any<Key>(), Arg.Any<JobId>()).Returns(true);
    }

    Task Because() => PerformStep();

    [Fact] void should_deliver_the_replay_batch() => _handledBatches.Count.ShouldEqual(1);
    [Fact] void should_not_check_the_replay_fence() => _observer.DidNotReceive().IsPartitionFenced(Arg.Any<Key>(), Arg.Any<JobId>());
    [Fact] void should_finish_successfully() => _succeeded.ShouldBeTrue();
}
