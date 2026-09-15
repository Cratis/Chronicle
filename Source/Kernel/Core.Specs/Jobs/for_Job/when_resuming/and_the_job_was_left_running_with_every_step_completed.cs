// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Monads;
using Moq;

namespace Cratis.Chronicle.Jobs.for_Job.when_resuming;

/// <summary>
/// Regression for https://github.com/Cratis/Chronicle/issues/3944 — a job persisted as Running with every step
/// already accounted for used to report itself as already running, so nothing ever finalized it and the observer
/// it belonged to could never resubscribe.
/// </summary>
public class and_the_job_was_left_running_with_every_step_completed : given.the_job
{
    JobStepId _jobStepId;
    Mock<given.ISomeJobStep> _jobStep;
    Result<ResumeJobSuccess, ResumeJobError> _result;

    void Establish()
    {
        _job.ShouldBeResumable = true;
        _jobStepId = Guid.Parse("3f3f3f3f-0000-0000-0000-000000000003");
        _jobStep = AddJobStep(_jobStepId);
        _jobStep.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Success());
    }

    async Task Because()
    {
        await _job.Start(new());

        // The state the store was found in: the progress for the last step was written, the status that follows
        // from it never was.
        _job.CurrentState.Status = JobStatus.Running;
        _job.CurrentState.Progress.TotalSteps = 1;
        _job.CurrentState.Progress.SuccessfulSteps = 1;
        _job.CurrentState.Progress.FailedSteps = 0;

        _result = await _job.Resume();
    }

    [Fact] void should_report_the_resume_as_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_the_job_as_completed() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.JobIsCompleted);
    [Fact] void should_finalize_the_job() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
    [Fact] void should_not_leave_the_job_running() => _job.CurrentState.Status.ShouldNotEqual(JobStatus.Running);
}
