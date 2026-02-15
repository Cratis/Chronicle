// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.InProcess.Integration.for_JobsManager.given;
using Cratis.Chronicle.Jobs;
using Cratis.Monads;
using context = Cratis.Chronicle.InProcess.Integration.for_JobsManager.when_starting.job_with_single_step.and_resuming_successfully_completed_job.context;

namespace Cratis.Chronicle.InProcess.Integration.for_JobsManager.when_starting.job_with_single_step;

[Collection(ChronicleCollection.Name)]
public class and_resuming_successfully_completed_job(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : a_jobs_manager(chronicleInProcessFixture)
    {
        public Result<Concepts.Jobs.JobId, StartJobError> StartJobResult;
        public Job CompletedJobState;
        public Concepts.Jobs.JobId JobId;
        public IEnumerable<JobStep> JobSteps;

        async Task Because()
        {
            JobStepProcessor.SetNumJobStepsToComplete(1);
            StartJobResult = await JobsManager.Start<IJobWithSingleStep, JobWithSingleStepRequest>(new() { KeepAfterCompleted = true });
            await JobStepProcessor.WaitForStepsToBeCompleted();
            JobId = StartJobResult.AsT0;
            var job = await EventStore.Jobs.GetJob(JobId.Value);
            var x = await EventStore.Jobs.WaitTillJobMeetsPredicate(JobId.Value, state => state.Status == JobStatus.CompletedSuccessfully, TimeSpanFactory.FromSeconds(20));
            await JobsManager.Resume(JobId);
            CompletedJobState = await EventStore.Jobs.WaitTillJobMeetsPredicate(JobId.Value, state => state.Status == JobStatus.CompletedSuccessfully && state.Progress.IsCompleted, TimeSpanFactory.FromSeconds(20));
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
    public void should_have_completed_work_successfully_for_one_job_step() => Context.JobStepProcessor.ShouldHaveCompletedJobSteps(Context.JobId, Concepts.Jobs.JobStepStatus.CompletedSuccessfully, 1);
}
