// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.InProcess.Integration.for_JobsManager.given;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Monads;
using Cratis.Chronicle.Storage.Jobs;
using context = Cratis.Chronicle.InProcess.Integration.for_JobsManager.when_starting.job_with_single_step.and_job_step_fails_and_keep_state_is_false.context;

namespace Cratis.Chronicle.InProcess.Integration.for_JobsManager.when_starting.job_with_single_step;

[Collection(ChronicleCollection.Name)]
public class and_job_step_fails_and_keep_state_is_false(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_jobs_manager(chronicleInProcessFixture)
    {
        public Result<Concepts.Jobs.JobId, StartJobError> StartJobResult;
        public Job? Job;
        public IEnumerable<JobStep> JobSteps;
        public Concepts.Jobs.JobId JobId;
        public Catch<JobState, Storage.Jobs.JobError> GetJobResult;

        async Task Because()
        {
            TheJobStepProcessor.SetNumJobStepsToComplete(1);
            StartJobResult = await JobsManager.Start<IJobWithSingleStep, JobWithSingleStepRequest>(new() { KeepAfterCompleted = false, ShouldFail = true });
            await TheJobStepProcessor.WaitForStepsToBeCompleted();
            JobId = StartJobResult.AsT0;
            Job = await EventStore.Jobs.GetJob(JobId.Value);
            JobSteps = await Job.GetJobSteps();
        }
    }

    [Fact]
    public void should_start_job() => Context.StartJobResult.IsSuccess.ShouldBeTrue();

    [Fact]
    public void should_keep_job_state() => Context.Job.ShouldNotBeNull();

    [Fact]
    public void should_have_correct_job_type() => Context.Job.Type.Value.ShouldEqual(nameof(JobWithSingleStep));

    [Fact]
    public void should_keep_state_of_failed_job_step() => Context.JobSteps.ShouldContainSingleItem();

    [Fact]
    public void should_have_job_step_state_where_status_is_failed() => Context.JobSteps.First().Status.ShouldEqual(JobStepStatus.CompletedWithFailure);

    [Fact]
    public void should_have_completed_job_progress() => Context.Job.Progress.IsCompleted.ShouldBeTrue();

    [Fact]
    public void should_have_job_progress_with_one_failed_step() => Context.Job.Progress.FailedSteps.ShouldEqual(1);

    [Fact]
    public void should_perform_work_for_job_step_only_once() => Context.TheJobStepProcessor.GetNumPerformCallsPerJobStep(Context.StartJobResult.AsT0).ShouldContainSingleItem();

    [Fact]
    public void should_have_completed_work_unsuccessfully_for_one_job_step() => Context.TheJobStepProcessor.ShouldHaveCompletedJobSteps(Context.JobId, Concepts.Jobs.JobStepStatus.CompletedWithFailure, 1);
}
