// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Integration.Orleans.InProcess.for_JobsManager.given;
using Cratis.Chronicle.Storage.Jobs;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_JobsManager.when_starting.job_with_single_step.and_job_step_completes_successfully.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_JobsManager.when_starting.job_with_single_step;

[Collection(GlobalCollection.Name)]
public class and_job_step_completes_successfully(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.all_dependencies(globalFixture)
    {
        public Result<JobId, StartJobError> StartJobResult;
        public JobState CompletedJobState;
        public IImmutableList<JobStepState> CompletedJobStepStates;

        async Task Because()
        {
            TheJobStepProcessor.SetNumJobStepsToComplete(1);
            StartJobResult = await JobsManager.Start<IJobWithSingleStep, JobWithSingleStepRequest>(new() { KeepAfterCompleted = true });
            await TheJobStepProcessor.WaitForAllStepsToBeCompleted();
            var jobId = StartJobResult.AsT0;
            var getJobState = await JobStorage.GetJob(jobId);
            var getJobStepState = await JobStepStorage.GetForJob(jobId);
            CompletedJobState = getJobState.AsT0;
            CompletedJobStepStates = getJobStepState.AsT0;
        }
    }

    [Fact]
    public void should_start_job() => Context.StartJobResult.IsSuccess.ShouldBeTrue();
}
