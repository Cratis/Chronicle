// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition.when_replay_completes;

public class and_the_replay_stopped_having_read_nothing : given.a_partition_replay_job
{
    void Establish() => _stateStorage.State.HandledAllEvents = false;

    async Task Because()
    {
        await _job.Start(_request);
        await _job.CompleteForTesting();
    }

    [Fact] void should_notify_observer_without_proving_any_event_type_replayed() => _observer.Received(1).PartitionReplayed((Key)"some-partition", EventSequenceNumber.Unavailable, EventSequenceNumber.Unavailable, Arg.Is<EventType[]>(_ => _.Length == 0));
}
