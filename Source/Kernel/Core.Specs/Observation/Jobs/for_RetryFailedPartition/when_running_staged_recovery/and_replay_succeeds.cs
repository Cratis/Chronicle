// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Moq;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition.when_running_staged_recovery;

public class and_replay_succeeds : given.a_staged_recovery
{
    async Task Because()
    {
        await _job.Start(_request);
        await _job.OnStepSucceeded(_replay.Id, JobStepResult.Succeeded(new HandleEventsForPartitionResult(5)));
    }

    [Fact] void should_start_replay_through_the_shared_job_engine() => _replayStep.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
    [Fact] void should_start_live_after_replay_succeeds() => _liveStep.Verify(step => step.Start(It.IsAny<GrainId>()), Times.Once);
}
