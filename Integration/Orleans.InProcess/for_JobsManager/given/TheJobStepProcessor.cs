// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.DependencyInjection;
namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_JobsManager.given;

[Singleton, IgnoreConvention]
public class TheJobStepProcessor
{
    readonly TaskCompletionSource _allPreparedJobsCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _numJobStepsToComplete;
    volatile int _numJobStepsCompleted;

    public readonly ConcurrentDictionary<JobId, (JobStepId JobStepId, Type JobStepType)> JobSteps = new();
    public readonly ConcurrentDictionary<JobId, Dictionary<JobStepId, IList<TheJobStepState>>> JobStepPerformCalls = new();
    public readonly ConcurrentDictionary<JobId, Dictionary<JobStepId, TheJobStepState>> CompletedJobSteps = new();

    public void SetNumJobStepsToComplete(int numJobSteps) => _numJobStepsToComplete = numJobSteps;

    public Task WaitForAllStepsToBeCompleted(TimeSpan? maxWaitTime = null) => _allPreparedJobsCompleted.Task.WaitAsync(maxWaitTime ?? TimeSpan.FromSeconds(10));

    public int GetNumPerformCallsFor(JobId jobId, JobStepId jobStepId)
    {
        if (!JobStepPerformCalls.TryGetValue(jobId, out var jobSteps))
        {
            return 0;
        }
        return !jobSteps.TryGetValue(jobStepId, out var jobStepCalls)
            ? 0
            : jobStepCalls.Count;
    }

#pragma warning disable MA0106
    public void JobStepPrepared(JobId JobId, JobStepId jobStepId, Type jobStepType) => JobSteps.AddOrUpdate(JobId, (jobStepId, jobStepType), (_, __) => (jobStepId, jobStepType));
#pragma warning restore MA0106

    public void PerformJobStep(JobId jobId, JobStepId jobStepId, TheJobStepState jobStepState)
    {
        JobStepPerformCalls.AddOrUpdate(
            jobId,
            new Dictionary<JobStepId, IList<TheJobStepState>>
            {
                {
                    jobStepId, new List<TheJobStepState>
                    {
                        jobStepState
                    }
                }
            },
#pragma warning disable MA0106
            (id, states) =>
            {
                if (!states.TryGetValue(jobStepId, out var jobStepStates))
                {
                    jobStepStates = [];
                }
                jobStepStates.Add(jobStepState);
                states[jobStepId] = jobStepStates;
                return states;
            });
#pragma warning restore MA0106
    }

    public void JobStepCompleted(JobId jobId, JobStepId jobStepId, TheJobStepState jobStepState)
    {
        CompletedJobSteps.AddOrUpdate(
            jobId,
            new Dictionary<JobStepId, TheJobStepState>
            {
                {
                    jobStepId, jobStepState
                }
            },
#pragma warning disable MA0106
            (id, states) =>
            {
                states.Add(jobStepId, jobStepState);
                return states;
            });
#pragma warning restore MA0106
        Interlocked.Increment(ref _numJobStepsCompleted);
        if (_numJobStepsCompleted >= _numJobStepsToComplete)
        {
            _allPreparedJobsCompleted.TrySetResult();
        }
    }
}
