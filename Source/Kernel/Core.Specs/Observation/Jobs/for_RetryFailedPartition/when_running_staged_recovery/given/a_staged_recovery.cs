// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Jobs.Stages;
using Moq;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition.when_running_staged_recovery.given;

public class a_staged_recovery : for_RetryFailedPartition.given.a_retry_failed_partition_job
{
    protected JobStepDetails _replay;
    protected JobStepDetails _live;
    protected Mock<IHandleEventsForPartition> _replayStep;
    protected Mock<IHandleEventsForPartition> _liveStep;

    void Establish()
    {
        _request = _request with
        {
            ObserverType = ObserverType.Reactor,
            RecoveryCursor = new(2, FailedPartitionRecoveryMode.Replay, 5),
            EpisodeId = FailedPartitionId.New()
        };
        _job.BeforePreparingSteps = steps =>
        {
            _replay = steps[0];
            _live = steps[1];
            _replayStep = Probe(_replay);
            _liveStep = Probe(_live);
        };
    }

    Mock<IHandleEventsForPartition> Probe(JobStepDetails step)
    {
        var probe = _silo.AddProbe<IHandleEventsForPartition>(step.Id, keyExtension: step.Key);
        probe.Setup(_ => _.Prepare(It.IsAny<object>(), It.IsAny<JobStepStage>()))
            .ReturnsAsync(Result<PrepareJobStepError>.Success());
        probe.Setup(_ => _.Start(It.IsAny<GrainId>()))
            .ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.AlreadyStarted));
        probe.Setup(_ => _.ReportStatusChange(It.IsAny<JobStepStatus>()))
            .ReturnsAsync(Result<JobStepError>.Success());
        return probe;
    }
}
