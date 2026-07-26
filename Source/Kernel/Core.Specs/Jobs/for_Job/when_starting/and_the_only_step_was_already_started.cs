// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Monads;
using Moq;

namespace Cratis.Chronicle.Jobs.for_Job.when_starting;

public class and_the_only_step_was_already_started : given.the_job
{
    JobStepId _jobStepId;
    Mock<given.ISomeJobStep> _jobStep;
    Result<StartJobError> _result;

    void Establish()
    {
        _jobStepId = Guid.Parse("1a1a1a1a-0000-0000-0000-000000000001");
        _jobStep = AddJobStep(_jobStepId);
        _jobStep.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.AlreadyStarted));
    }

    async Task Because() => _result = await _job.Start(new());

    [Fact] void should_consider_the_job_started() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_count_the_step_as_failed() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(0);
    [Fact] void should_leave_the_job_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_record_the_step_as_failed() => StoredJobStepsWith(JobStepStatus.Failed).ShouldEqual(_job.CurrentState.Progress.FailedSteps);
}
