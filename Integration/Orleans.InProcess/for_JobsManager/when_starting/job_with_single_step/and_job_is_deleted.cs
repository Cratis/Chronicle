// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Integration.Orleans.InProcess.for_JobsManager.given;
using Cratis.Chronicle.Storage.Jobs;

using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_JobsManager.when_starting.job_with_single_step.and_job_is_deleted.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_JobsManager.when_starting.job_with_single_step;

[Collection(GlobalCollection.Name)]
public class and_job_is_deleted(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.a_jobs_manager(globalFixture)
    {
        public Result<JobId, StartJobError> StartJobResult;
        public IImmutableList<JobStepState> JobStepStates;
        public JobId JobId;

        async Task Because()
        {
            var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            TheJobStepProcessor.SetStartTask(taskCompletionSource.Task);
            StartJobResult = await JobsManager.Start<IJobWithSingleStep, JobWithSingleStepRequest>(new() { KeepAfterCompleted = true });
            JobId = StartJobResult.AsT0;
            await TheJobStepProcessor.WaitForAllPreparedStepsToBeStarted();
            await JobsManager.Delete(JobId);
            taskCompletionSource.SetResult();
            await JobStorage.WaitTillJobIsDeleted<JobWithSingleStepState>(JobId);
            var getJobStepState = await JobStepStorage.GetForJob(JobId);
            JobStepStates = getJobStepState.AsT0;
        }
    }

    [Fact]
    public void should_start_job() => Context.StartJobResult.IsSuccess.ShouldBeTrue();

    [Fact]
    public void should_remove_state_of_job_step() => Context.JobStepStates.ShouldBeEmpty();

    [Fact]
    public void should_perform_work_for_job_step_only_once() => Context.TheJobStepProcessor.GetNumPerformCallsPerJobStep(Context.StartJobResult.AsT0).ShouldContainSingleItem();

    [Fact]
    public void should_have_stopped_work_for_one_job_step() => Context.TheJobStepProcessor.ShouldHaveCompletedJobSteps(Context.JobId, JobStepStatus.Stopped, 1);
}
