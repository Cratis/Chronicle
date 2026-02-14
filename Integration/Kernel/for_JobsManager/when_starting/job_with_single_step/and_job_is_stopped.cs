// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Integration.Specifications.for_JobsManager.given;
using Cratis.Chronicle.Jobs;
using Cratis.Monads;
using context = Cratis.Chronicle.Kernel.Integration.for_JobsManager.when_starting.job_with_single_step.and_job_is_stopped.context;

namespace Cratis.Chronicle.Kernel.Integration.for_JobsManager.when_starting.job_with_single_step;

[Collection(ChronicleCollection.Name)]
public class and_job_is_stopped(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : a_jobs_manager(chronicleInProcessFixture)
    {
        public Result<Concepts.Jobs.JobId, StartJobError> StartJobResult;
        public Job CompletedJobState;
        public IEnumerable<JobStep> JobSteps;
        public Concepts.Jobs.JobId JobId;

        async Task Because()
        {
            var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            JobStepProcessor.SetStartTask(taskCompletionSource.Task);
            StartJobResult = await JobsManager.Start<IJobWithSingleStep, JobWithSingleStepRequest>(new() { KeepAfterCompleted = true });
            JobId = StartJobResult.AsT0;
            var job = await EventStore.Jobs.GetJob(JobId.Value);
            await JobStepProcessor.WaitForAllPreparedStepsToBeStarted();
            await JobsManager.Stop(JobId);
            taskCompletionSource.SetResult();
            CompletedJobState = await EventStore.Jobs.WaitTillJobProgressStopped(JobId.Value);
            JobSteps = await job.GetJobSteps();
        }
    }

    [Fact]
    public void should_start_job() => Context.StartJobResult.IsSuccess.ShouldBeTrue();

    [Fact]
    public void should_have_correct_job_type() => Context.CompletedJobState.Type.Value.ShouldEqual(nameof(JobWithSingleStep));

    [Fact]
    public void should_not_remove_state_of_job_step() => Context.JobSteps.ShouldNotBeEmpty();

    [Fact]
    public void should_have_stopped_job_status() => Context.CompletedJobState.Status.ShouldEqual(JobStatus.Stopped);

    [Fact]
    public void should_have_stopped_job_progress() => Context.CompletedJobState.Progress.IsStopped.ShouldBeTrue();

    [Fact]
    public void should_have_not_completed_job_progress() => Context.CompletedJobState.Progress.IsCompleted.ShouldBeFalse();

    [Fact]
    public void should_have_job_progress_with_one_stopped_step() => Context.CompletedJobState.Progress.StoppedSteps.ShouldEqual(1);

    [Fact]
    public void should_perform_work_for_job_step_only_once() => Context.JobStepProcessor.GetNumPerformCallsPerJobStep(Context.StartJobResult.AsT0).ShouldContainSingleItem();

    [Fact]
    public void should_have_stopped_work_for_one_job_step() => Context.JobStepProcessor.ShouldHaveCompletedJobSteps(Context.JobId, Concepts.Jobs.JobStepStatus.Stopped, 1);
}
