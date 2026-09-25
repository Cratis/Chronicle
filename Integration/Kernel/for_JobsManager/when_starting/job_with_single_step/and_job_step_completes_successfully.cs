// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Specifications.for_JobsManager.given;
using Cratis.Chronicle.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using context = Cratis.Chronicle.Kernel.Integration.for_JobsManager.when_starting.job_with_single_step.and_job_step_completes_successfully.context;
using JobStatus = Cratis.Chronicle.Jobs.JobStatus;

namespace Cratis.Chronicle.Kernel.Integration.for_JobsManager.when_starting.job_with_single_step;

[Collection(ChronicleCollection.Name)]
public class and_job_step_completes_successfully(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : a_jobs_manager(chronicleInProcessFixture)
    {
        public Result<Cratis.Orleans.Jobs.JobId, StartJobError> StartJobResult;
        public Job CompletedJobState;
        public Cratis.Orleans.Jobs.JobId JobId;
        public IEnumerable<JobStep> JobSteps;

        async Task Because()
        {
            JobStepProcessor.SetNumJobStepsToComplete(1);
            StartJobResult = await JobsManager.Start<IJobWithSingleStep, JobWithSingleStepRequest>(new() { KeepAfterCompleted = true });
            await JobStepProcessor.WaitForStepsToBeCompleted();
            JobId = StartJobResult.AsT0;
            var job = await EventStore.Jobs.GetJob(JobId.Value);
            CompletedJobState = await EventStore.Jobs.WaitTillJobMeetsPredicate(JobId.Value, state => state.Status == JobStatus.CompletedSuccessfully);
            JobSteps = await job.GetJobSteps();
        }
    }

    [Fact]
    public void should_start_job() => Context.StartJobResult.IsSuccess.ShouldBeTrue();

    [Fact]
    public void should_have_correct_job_type() => Context.CompletedJobState.Type.Value.ShouldEqual(nameof(JobWithSingleStep));

    [Fact]
    public void should_not_keep_any_job_step_states_after_completed() => Context.JobSteps.ShouldBeEmpty();

    [Fact]
    public void should_have_completed_job_successfully() => Context.CompletedJobState.Status.ShouldEqual(JobStatus.CompletedSuccessfully);

    [Fact]
    public void should_have_completed_job_progress() => Context.CompletedJobState.Progress.IsCompleted.ShouldBeTrue();

    [Fact]
    public void should_have_job_progress_with_one_successful_step() => Context.CompletedJobState.Progress.SuccessfulSteps.ShouldEqual(1);

    [Fact]
    public void should_perform_work_for_job_step_only_once() => Context.JobStepProcessor.ShouldHavePerformedJobStepCalls(Context.JobId, 1);

    [Fact]
    public void should_have_completed_work_successfully_for_one_job_step() => Context.JobStepProcessor.ShouldHaveCompletedJobSteps(Context.JobId, Cratis.Orleans.Jobs.JobStepStatus.CompletedSuccessfully, 1);
}
