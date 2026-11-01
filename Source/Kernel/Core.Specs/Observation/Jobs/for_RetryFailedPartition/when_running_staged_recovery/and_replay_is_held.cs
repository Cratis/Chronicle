// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;
using Moq;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition.when_running_staged_recovery;

public class and_replay_is_held : given.a_staged_recovery
{
    async Task Because()
    {
        await _job.Start(_request);
        await _job.OnStepStopped(_replay.Id, JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult()));
    }

    [Fact] void should_start_replay_through_the_shared_job_engine() => _replayStep.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_not_start_live() => _liveStep.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Never);
    [Fact] void should_not_clear_the_episode() => _observer.DidNotReceive().FailedPartitionRecovered(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<FailedPartitionId?>(), Arg.Any<Guid?>());
}
