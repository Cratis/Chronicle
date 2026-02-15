// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.for_JobsManager.given;
using Cratis.Chronicle.Jobs;
using Cratis.Monads;
using context = Cratis.Chronicle.InProcess.Integration.for_JobsManager.when_starting.job_with_single_step.and_job_step_fails.context;

namespace Cratis.Chronicle.InProcess.Integration.for_JobsManager.when_starting.job_with_single_step;

[Collection(ChronicleCollection.Name)]
public class and_job_step_fails(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : a_jobs_manager(chronicleInProcessFixture)
    {
        public Result<Concepts.Jobs.JobId, StartJobError> StartJobResult;
        public Job CompletedJobState;
        public IEnumerable<JobStep> JobStep;
        public Concepts.Jobs.JobId JobId;

        async Task Because()
        {
            JobStepProcessor.SetNumJobStepsToComplete(1);
            StartJobResult = await JobsManager.Start<IJobWithSingleStep, JobWithSingleStepRequest>(new() { KeepAfterCompleted = true, ShouldFail = true });
            await JobStepProcessor.WaitForStepsToBeCompleted();
            JobId = StartJobResult.AsT0;
            var getJobState = await JobStorage.GetJob(JobId);
            var getJobStepState = await JobStepStorage.GetForJob(JobId);

            CompletedJobState = await EventStore.Jobs.WaitTillJobProgressCompleted(JobId.Value);
            JobStep = await CompletedJobState.GetJobSteps();
            CompletedJobState = await EventStore.Jobs.WaitTillJobMeetsPredicate(JobId.Value, state => state.Status is JobStatus.CompletedWithFailures);
        }
    }

    [Fact]
    public void should_start_job() => Context.StartJobResult.IsSuccess.ShouldBeTrue();

    [Fact]
    public void should_keep_job_state() => Context.CompletedJobState.ShouldNotBeNull();

    [Fact]
    public void should_have_correct_job_type() => Context.CompletedJobState.Type.Value.ShouldEqual(nameof(JobWithSingleStep));

    [Fact]
    public void should_keep_state_of_failed_job_step() => Context.JobStep.ShouldContainSingleItem();

    [Fact]
    public void should_have_job_step_state_where_status_is_failed() => Context.JobStep.First().Status.ShouldEqual(JobStepStatus.CompletedWithFailure);

    [Fact]
    public void should_have_completed_job_with_failure() => Context.CompletedJobState.Status.ShouldEqual(JobStatus.CompletedWithFailures);

    [Fact]
    public void should_have_completed_job_progress() => Context.CompletedJobState.Progress.IsCompleted.ShouldBeTrue();

    [Fact]
    public void should_have_job_progress_with_one_failed_step() => Context.CompletedJobState.Progress.FailedSteps.ShouldEqual(1);

    [Fact]
    public void should_perform_work_for_job_step_only_once() => Context.JobStepProcessor.GetNumPerformCallsPerJobStep(Context.StartJobResult.AsT0).ShouldContainSingleItem();

    [Fact]
    public void should_have_completed_work_unsuccessfully_for_one_job_step() => Context.JobStepProcessor.ShouldHaveCompletedJobSteps(Context.JobId, Concepts.Jobs.JobStepStatus.CompletedWithFailure, 1);
}
