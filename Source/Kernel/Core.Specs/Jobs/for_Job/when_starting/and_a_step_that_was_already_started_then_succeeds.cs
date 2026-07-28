// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Monads;
using Moq;

namespace Cratis.Chronicle.Jobs.for_Job.when_starting;

public class and_a_step_that_was_already_started_then_succeeds : given.the_job
{
    JobStepId _jobStepId;
    Mock<given.ISomeJobStep> _jobStep;

    void Establish()
    {
        _jobStepId = Guid.Parse("1a1a1a1a-0000-0000-0000-000000000003");
        _jobStep = AddJobStep(_jobStepId);
        _jobStep.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.AlreadyStarted));
    }

    async Task Because()
    {
        await _job.Start(new());
        await _job.OnStepSucceeded(_jobStepId, JobStepResult.Succeeded());
    }

    [Fact] void should_complete_the_job_successfully() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
    [Fact] void should_count_the_step_as_successful() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_not_count_any_step_as_failed() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(0);
    [Fact] void should_have_as_many_failed_steps_recorded_as_counted() => StoredJobStepsWith(JobStepStatus.Failed).ShouldEqual(_job.CurrentState.Progress.FailedSteps);
}
