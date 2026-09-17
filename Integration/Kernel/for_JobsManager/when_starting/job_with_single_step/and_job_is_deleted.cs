// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Specifications.for_JobsManager.given;
using Cratis.Chronicle.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using context = Cratis.Chronicle.Kernel.Integration.for_JobsManager.when_starting.job_with_single_step.and_job_is_deleted.context;

namespace Cratis.Chronicle.Kernel.Integration.for_JobsManager.when_starting.job_with_single_step;

[Collection(ChronicleCollection.Name)]
public class and_job_is_deleted(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : a_jobs_manager(chronicleInProcessFixture)
    {
        public Result<Cratis.Orleans.Jobs.JobId, StartJobError> StartJobResult;
        public IEnumerable<JobStep> JobSteps;
        public Cratis.Orleans.Jobs.JobId JobId;

        async Task Because()
        {
            var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            JobStepProcessor.SetStartTask(taskCompletionSource.Task);
            StartJobResult = await JobsManager.Start<IJobWithSingleStep, JobWithSingleStepRequest>(new() { KeepAfterCompleted = true });
            JobId = StartJobResult.AsT0;
            var job = await EventStore.Jobs.GetJob(JobId.Value);
            await JobStepProcessor.WaitForAllPreparedStepsToBeStarted();
            await JobsManager.Delete(JobId);
            taskCompletionSource.SetResult();
            await EventStore.Jobs.WaitTillJobIsDeleted(JobId.Value);
            JobSteps = await job.GetJobSteps();
        }
    }

    [Fact]
    public void should_start_job() => Context.StartJobResult.IsSuccess.ShouldBeTrue();

    [Fact]
    public void should_remove_state_of_job_step() => Context.JobSteps.ShouldBeEmpty();

    [Fact]
    public void should_perform_work_for_job_step_only_once() => Context.JobStepProcessor.GetNumPerformCallsPerJobStep(Context.StartJobResult.AsT0).ShouldContainSingleItem();

    [Fact]
    public void should_have_stopped_work_for_one_job_step() => Context.JobStepProcessor.ShouldHaveCompletedJobSteps(Context.JobId, Cratis.Orleans.Jobs.JobStepStatus.Stopped, 1);
}
