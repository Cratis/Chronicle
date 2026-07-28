// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Monads;
using Moq;

namespace Cratis.Chronicle.Jobs.for_Job.when_starting;

public class and_one_step_fails_to_start_while_another_was_already_started : given.the_job
{
    JobStepId _alreadyStartedJobStepId;
    JobStepId _failingJobStepId;
    Mock<given.ISomeJobStep> _alreadyStartedJobStep;
    Mock<given.ISomeJobStep> _failingJobStep;
    Result<StartJobError> _result;

    void Establish()
    {
        _alreadyStartedJobStepId = Guid.Parse("1a1a1a1a-0000-0000-0000-000000000005");
        _failingJobStepId = Guid.Parse("1a1a1a1a-0000-0000-0000-000000000006");
        _alreadyStartedJobStep = AddJobStep(_alreadyStartedJobStepId);
        _failingJobStep = AddJobStep(_failingJobStepId);
        _alreadyStartedJobStep.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.AlreadyStarted));
        _failingJobStep.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Failed(StartJobStepError.NotPrepared));
    }

    async Task Because() => _result = await _job.Start(new());

    [Fact] void should_report_that_only_some_steps_failed_starting() => ((StartJobError)_result).ShouldEqual(StartJobError.FailedStartingSomeJobSteps);
    [Fact] void should_only_count_the_genuinely_failed_step() => _job.CurrentState.Progress.FailedSteps.ShouldEqual(1);
    [Fact] void should_record_the_failing_step_as_failed() => _failingJobStep.Verify(_ => _.ReportStatusChange(JobStepStatus.Failed), Times.Once);
    [Fact] void should_not_record_the_already_started_step_as_failed() => _alreadyStartedJobStep.Verify(_ => _.ReportStatusChange(JobStepStatus.Failed), Times.Never);
    [Fact] void should_have_as_many_failed_steps_recorded_as_counted() => StoredJobStepsWith(JobStepStatus.Failed).ShouldEqual(_job.CurrentState.Progress.FailedSteps);
}
