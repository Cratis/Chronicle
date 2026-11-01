// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Jobs.Stages;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition.when_preparing_steps;

public class and_recovery_is_within_a_replay : given.a_retry_failed_partition_job
{
    IImmutableList<JobStepDetails> _steps;

    HandleEventsForPartitionArguments Replay => (HandleEventsForPartitionArguments)_steps[0].Request;
    HandleEventsForPartitionArguments Live => (HandleEventsForPartitionArguments)_steps[1].Request;

    void Establish() => _request = _request with
    {
        ObserverType = ObserverType.Reactor,
        FromSequenceNumber = 2,
        RecoveryCursor = new(2, FailedPartitionRecoveryMode.Replay, 5),
        EpisodeId = FailedPartitionId.New()
    };

    async Task Because() => _steps = await _job.PrepareRecoverySteps(_request);

    [Fact] void should_prepare_replay_then_live() => _steps.Count.ShouldEqual(2);
    [Fact] void should_retain_the_exact_replay_position() => Replay.StartEventSequenceNumber.ShouldEqual((EventSequenceNumber)2);
    [Fact] void should_retain_the_original_replay_boundary() => Replay.EndEventSequenceNumber.ShouldEqual((EventSequenceNumber)5);
    [Fact] void should_replay_the_historical_events() => Replay.EventObservationState.ShouldEqual(EventObservationState.Replay);
    [Fact] void should_put_replay_in_the_first_stage() => _steps[0].Stage.ShouldEqual(JobStepStage.First);
    [Fact] void should_put_live_in_the_next_stage() => _steps[1].Stage.ShouldEqual((JobStepStage)1);
    [Fact] void should_start_live_strictly_after_the_boundary() => Live.StartEventSequenceNumber.ShouldEqual((EventSequenceNumber)6);
    [Fact] void should_deliver_new_events_live() => Live.EventObservationState.ShouldEqual(EventObservationState.None);
    [Fact] void should_read_new_events_to_the_tail() => Live.EndEventSequenceNumber.ShouldEqual(EventSequenceNumber.Max);
    [Fact] void should_fence_replay_to_the_episode() => Replay.EpisodeId.ShouldEqual(_request.EpisodeId);
    [Fact] void should_fence_live_to_the_same_episode() => Live.EpisodeId.ShouldEqual(_request.EpisodeId);
}
