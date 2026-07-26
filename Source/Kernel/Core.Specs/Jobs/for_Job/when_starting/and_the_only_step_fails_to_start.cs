// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Monads;
using Moq;

namespace Cratis.Chronicle.Jobs.for_Job.when_starting;

public class and_the_only_step_fails_to_start : given.the_job
{
    JobStepId _jobStepId;
    Mock<given.ISomeJobStep> _jobStep;
    Result<StartJobError> _result;

    void Establish()
    {
        _jobStepId = Guid.Parse("1a1a1a1a-0000-0000-0000-000000000004");
        _jobStep = AddJobStep(_jobStepId);
        _jobStep.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.NotPrepared));
    }

    async Task Because() => _result = await _job.Start(new());

    [Fact] void should_report_that_all_steps_failed_starting() => ((StartJobError)_result).ShouldEqual(StartJobError.AllJobStepsFailedStarting);
    [Fact] void should_count_the_step_as_failed() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(1);
    [Fact] void should_complete_the_job_with_failures() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedWithFailures);
    [Fact] void should_record_the_step_as_failed() => _jobStep.Verify(_ => _.ReportStatusChange(JobStepStatus.Failed), Times.Once);
    [Fact] void should_have_as_many_failed_steps_recorded_as_counted() => StoredJobStepsWith(JobStepStatus.Failed).ShouldEqual(_job.CurrentState.Progress.FailedSteps);
}
