// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Monads;
using Moq;

namespace Cratis.Chronicle.Jobs.for_Job.when_a_step_completes;

public class and_the_step_is_no_longer_tracked : given.the_job
{
    JobStepId _firstJobStepId;
    JobStepId _secondJobStepId;
    JobStepId _untrackedJobStepId;
    Result<JobError> _result;

    void Establish()
    {
        _firstJobStepId = Guid.Parse("2b2b2b2b-0000-0000-0000-000000000001");
        _secondJobStepId = Guid.Parse("2b2b2b2b-0000-0000-0000-000000000002");
        _untrackedJobStepId = Guid.Parse("2b2b2b2b-0000-0000-0000-00000000000f");

        // Both steps report AlreadyStarted so the job reaches Running without the grain
        // being subscribed to - Orleans TestKit cannot hand out a real grain reference.
        foreach (var jobStepId in new[] { _firstJobStepId, _secondJobStepId })
        {
            AddJobStep(jobStepId)
                .Setup(_ => _.Start(It.IsAny<GrainId>()))
                .ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.AlreadyStarted));
        }
    }

    async Task Because()
    {
        await _job.Start(new());
        _result = await _job.OnStepSucceeded(_untrackedJobStepId, JobStepResult.Succeeded());
    }

    [Fact] void should_not_report_an_error() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_count_the_step_as_successful() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_leave_the_job_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_change_the_total_step_count() => _job.CurrentState.Progress.TotalSteps.ShouldEqual(2);
}
