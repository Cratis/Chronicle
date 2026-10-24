// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_a_partition_replay_fences_the_first_batch : given.a_performing_job_step_with_observer_filters
{
    void Establish()
    {
        _performState.LastSuccessfullyHandledEventSequenceNumber = 7UL;
        _eventCursor.Current.Returns([CreateEvent(8UL, _audited)]);
        _observer.IsPartitionFenced(Arg.Any<Key>(), Arg.Any<JobId>()).Returns(true);
    }

    Task Because() => PerformStep();

    [Fact] void should_not_deliver_the_fenced_batch() => _handledBatches.ShouldBeEmpty();
    [Fact] void should_finish_successfully() => _succeeded.ShouldBeTrue();
    [Fact] void should_hand_catchup_ownership_to_the_partition_replay() => _result.FencedByPartitionReplay.ShouldBeTrue();
    [Fact] void should_preserve_the_last_handled_position() => _result.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)7UL);
    [Fact] void should_not_claim_to_have_scanned_the_fenced_batch() => _result.LastScannedEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_advance_the_step_checkpoint() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
