// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Integration.Orleans.InProcess.for_JobsManager.given;
using Cratis.Chronicle.Storage.Jobs;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_JobsManager.when_starting.job_with_single_step.and_job_step_fails.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_JobsManager.when_starting.job_with_single_step;

[Collection(GlobalCollection.Name)]
public class and_job_step_fails(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.all_dependencies(globalFixture)
    {
        public Result<JobId, StartJobError> StartJobResult;
        public JobState CompletedJobState;
        public IImmutableList<JobStepState> JobStepStates;
        public JobId JobId;

        async Task Because()
        {
            TheJobStepProcessor.SetNumJobStepsToComplete(1);
            StartJobResult = await JobsManager.Start<IJobWithSingleStep, JobWithSingleStepRequest>(new() { KeepAfterCompleted = true, ShouldFail = true });
            await TheJobStepProcessor.WaitForStepsToBeCompleted();
            JobId = StartJobResult.AsT0;
            var getJobState = await JobStorage.GetJob(JobId);
            var getJobStepState = await JobStepStorage.GetForJob(JobId);
            CompletedJobState = getJobState.AsT0;
            JobStepStates = getJobStepState.AsT0;
        }
    }

    [Fact]
    public void should_start_job() => Context.StartJobResult.IsSuccess.ShouldBeTrue();

    [Fact]
    public void should_have_correct_job_type() => Context.CompletedJobState.Type.Value.ShouldEqual(nameof(JobWithSingleStep));

    [Fact]
    public void should_keep_state_of_failed_job_step() => Context.JobStepStates.ShouldContainSingleItem();

    [Fact]
    public void should_have_job_step_state_where_status_is_failed() => Context.JobStepStates[0].Status.ShouldEqual(JobStepStatus.Failed);

    [Fact]
    public void should_have_completed_job_with_failure() => Context.CompletedJobState.Status.ShouldEqual(JobStatus.CompletedWithFailures);

    [Fact]
    public void should_have_completed_job_progress() => Context.CompletedJobState.Progress.IsCompleted.ShouldBeTrue();

    [Fact]
    public void should_have_job_progress_with_one_failed_step() => Context.CompletedJobState.Progress.FailedSteps.ShouldEqual(1);

    [Fact]
    public void should_perform_work_for_job_step_only_once() => Context.TheJobStepProcessor.GetNumPerformCallsPerJobStep(Context.StartJobResult.AsT0).ShouldContainSingleItem();

    [Fact]
    public void should_have_completed_work_unsuccessfully_for_one_job_step() => Context.TheJobStepProcessor.ShouldHaveCompletedJobSteps(Context.JobId, JobStepStatus.Failed, 1);
}
