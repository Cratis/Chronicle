// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Integration.Specifications.for_JobsManager.given;
using Cratis.Chronicle.Jobs;
using Cratis.Monads;
using context = Cratis.Chronicle.Kernel.Integration.for_JobsManager.when_starting.job_with_single_step.and_then_resumed.context;

namespace Cratis.Chronicle.Kernel.Integration.for_JobsManager.when_starting.job_with_single_step;

[Collection(ChronicleCollection.Name)]
public class and_then_resumed(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : a_jobs_manager(chronicleInProcessFixture)
    {
        public Result<Concepts.Jobs.JobId, StartJobError> StartJobResult;
        public Job CompletedJobState;
        public Concepts.Jobs.JobId JobId;
        public IEnumerable<JobStep> JobSteps;

        async Task Because()
        {
            var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            JobStepProcessor.SetStartTask(taskCompletionSource.Task);
            StartJobResult = await JobsManager.Start<IJobWithSingleStep, JobWithSingleStepRequest>(new() { KeepAfterCompleted = true });
            JobId = StartJobResult.AsT0;
            await JobStepProcessor.WaitForAllPreparedStepsToBeStarted();
            await EventStore.Jobs.Resume(JobId.Value);
            taskCompletionSource.SetResult();
            CompletedJobState = await EventStore.Jobs.WaitTillJobMeetsPredicate(JobId.Value, state => state.Status == JobStatus.CompletedSuccessfully);
            JobSteps = await CompletedJobState.GetJobSteps();
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
