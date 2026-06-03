// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
namespace Cratis.Chronicle.Integration.Specifications.for_JobsManager.given;

public static class TheJobStepProcessorHelpers
{
    public static void ShouldHaveCompletedJobSteps(this TheJobStepProcessor.CompletedJobSteps completedJobSteps, JobStepStatus expectedStatus = JobStepStatus.Unknown, int? numberOfSucceededSteps = null)
    {
        var successfullyCompletedJobSteps = completedJobSteps.Count(_ => expectedStatus == JobStepStatus.Unknown || _.Value.Status == expectedStatus);
        successfullyCompletedJobSteps.ShouldEqual(numberOfSucceededSteps ?? completedJobSteps.Count);
    }

    public static void ShouldHaveCompletedJobSteps(this TheJobStepProcessor processor, JobId jobId, JobStepStatus expectedStatus = JobStepStatus.Unknown, int? numberOfSucceededSteps = null)
    {
        processor.CompletedSteps.Keys.ShouldContain(jobId);
        processor.CompletedSteps[jobId].ShouldHaveCompletedJobSteps(expectedStatus, numberOfSucceededSteps);
    }

    public static void ShouldHavePerformedJobStepCalls(this TheJobStepProcessor.PerformedJobStepCalls performedCalls, int? numberOfTimes = null, JobStepId? jobStepId = null)
    {
#pragma warning disable RCS1238
        var numCalls = jobStepId is not null
            ? performedCalls.TryGetValue(jobStepId, out var callsForJobStep) ? callsForJobStep.Count : 0
            : performedCalls.Sum(calls => calls.Value.Count);
#pragma warning restore RCS1238
        if (numberOfTimes.HasValue)
        {
            numCalls.ShouldEqual(numberOfTimes.Value);
        }
        else
        {
            numCalls.ShouldBeGreaterThan(0);
        }
    }

    public static void ShouldHavePerformedJobStepCalls(this TheJobStepProcessor processor, JobId jobId, int? numberOfTimes = null, JobStepId? jobStepId = null)
    {
        processor.JobStepPerformCalls.Keys.ShouldContain(jobId);
        processor.JobStepPerformCalls[jobId].ShouldHavePerformedJobStepCalls(numberOfTimes, jobStepId);
    }
}
