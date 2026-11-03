// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition.when_replay_completes;

public class and_an_isolated_rebuild_was_never_published : given.a_partition_replay_job
{
    void Establish()
    {
        // A rebuild step reports nothing handled until its publish succeeds, so an unpublished rebuild leaves no coverage.
        _partitionRebuildAdmission.IsAdmitted(Arg.Any<ReplayObserverPartitionRequest>()).Returns(true);
        _stateStorage.State.LastHandledEventSequenceNumber = EventSequenceNumber.Unavailable;
        _stateStorage.State.HandledAllEvents = false;
    }

    async Task Because()
    {
        await _job.Start(_request);
        await _job.CompleteForTesting();
    }

    [Fact] void should_not_claim_any_event_was_replayed() => _observer.Received(1).PartitionReplayed((Key)"some-partition", EventSequenceNumber.Unavailable, 44UL, Arg.Is<EventType[]>(_ => _.Length == 0));
}
