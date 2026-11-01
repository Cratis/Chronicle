// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_completing_partition_replay;

public class and_state_is_reloaded_with_an_additional_completion : given.a_partition_replay_covering_a_failure
{
    readonly Key _otherPartition = "other-partition";

    void Establish() => _failureStorage.FailOnWrite = 1;

    async Task Because()
    {
        await CompleteWithFault();
        _completionStorage.State = _completionStorage.PersistedState with
        {
            ReplayingPartitions = new HashSet<Key>([_partition, _otherPartition]),
            PartitionReplayCompletions =
            [
                new(_partition, 90UL, 100UL, null, DateTimeOffset.UnixEpoch),
                new(_otherPartition, 70UL, 80UL, null, DateTimeOffset.UnixEpoch)
            ]
        };
        await _completionStorage.WriteStateAsync();
        EventSequenceHasNextEvent(80UL);
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [_eventType], SiloAddress.Zero);
    }

    [Fact] void should_keep_the_in_memory_boundary() => CheckSuccessor();
    [Fact] void should_merge_the_missing_completion() => _jobsManager.Received(1)
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(_ =>
            _.Key == _otherPartition && _.FromSequenceNumber == (EventSequenceNumber)81UL));
    [Fact] void should_clear_both_persisted_intents() => _completionStorage.PersistedState.PartitionReplayCompletions.ShouldBeNull();
}
