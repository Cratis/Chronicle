// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Monads;
using Moq;

namespace Cratis.Chronicle.Jobs.for_Job.when_resuming;

public class and_a_step_fails_to_start : given.the_job
{
    JobStepId _jobStepId;
    Mock<given.ISomeJobStep> _jobStep;
    Result<ResumeJobSuccess, ResumeJobError> _result;

    void Establish()
    {
        _job.ShouldBeResumable = true;
        _jobStepId = Guid.Parse("2b2b2b2b-0000-0000-0000-000000000001");
        _jobStep = AddJobStep(_jobStepId);
        _jobStep.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.NotPrepared));
    }

    async Task Because()
    {
        await _job.Start(new());
        _job.CurrentState.Status = JobStatus.Stopped;
        _result = await _job.Resume();
    }

    [Fact] void should_not_report_the_resume_as_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_step_as_failed_to_resume() => ((FailedResumingJobSteps)(ResumeJobError)_result).FailedJobSteps.ShouldContainOnly(_jobStepId);
}
