// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.DependencyInjection;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Integration.Specifications.for_JobsManager.given;

[Singleton, IgnoreConvention]
public class TheJobStepProcessor
{
    public class PreparedJobSteps(IEnumerable<(JobStepId JobStepId, Type JobStepType)> list) : List<(JobStepId JobStepId, Type JobStepType)>(list);

    public class PerformedJobStepCalls : Dictionary<JobStepId, IList<TheJobStepState>>;
    public class CompletedJobSteps : Dictionary<JobStepId, (TheJobStepState State, JobStepStatus Status)>;

    readonly TaskCompletionSource _allPreparedJobsStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _jobsCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    Task _startTask = Task.CompletedTask;
    int _numJobStepsToComplete;

    volatile int _numJobStepsPrepared;
    volatile int _numJobStepsStarted;
    volatile int _numJobStepsCompleted;

    public readonly ConcurrentDictionary<JobId, PreparedJobSteps> JobSteps = new();
    public readonly ConcurrentDictionary<JobId, PerformedJobStepCalls> JobStepPerformCalls = new();
    public readonly ConcurrentDictionary<JobId, CompletedJobSteps> CompletedSteps = new();

    public void SetNumJobStepsToComplete(int numJobSteps) => _numJobStepsToComplete = numJobSteps;
    public void SetStartTask(Task startTask) => _startTask = startTask;
    public Task WaitForStart() => _startTask;
    public Task WaitForAllPreparedStepsToBeStarted(TimeSpan? maxWaitTime = null) => _allPreparedJobsStarted.Task.WaitAsync(maxWaitTime ?? TimeSpanFactory.FromSeconds(10));
    public Task WaitForStepsToBeCompleted(TimeSpan? maxWaitTime = null) => _jobsCompleted.Task.WaitAsync(maxWaitTime ?? TimeSpanFactory.FromSeconds(10));

    public IEnumerable<JobStepId> GetJobStepsForJob(JobId jobId)
    {
        if (!JobSteps.TryGetValue(jobId, out var jobSteps))
        {
            return [];
        }
        return jobSteps.Select(_ => _.JobStepId);
    }

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

    public Dictionary<JobStepId, int> GetNumPerformCallsPerJobStep(JobId jobId)
    {
        if (!JobStepPerformCalls.TryGetValue(jobId, out var jobSteps))
        {
            return [];
        }
        return jobSteps.ToDictionary(_ => _.Key, _ => _.Value.Count);
    }

    public CompletedJobSteps GetCompletedJobSteps(JobId jobId)
    {
        if (!CompletedSteps.TryGetValue(jobId, out var completedJobSteps))
        {
            return [];
        }
        return completedJobSteps;
    }

    public void JobStepPrepared(JobId JobId, JobStepId jobStepId, Type jobStepType)
    {
        JobSteps.AddOrUpdate(
            JobId,
            static (_, step) => new PreparedJobSteps([step]),
            static (_, jobSteps, step) => new(jobSteps.Concat([step])),
            (jobStepId, jobStepType));
        Interlocked.Increment(ref _numJobStepsPrepared);
    }

    public void PerformJobStep(JobId jobId, JobStepId jobStepId, TheJobStepState jobStepState)
    {
        JobStepPerformCalls.AddOrUpdate(
            jobId,
            static (_, step) => new PerformedJobStepCalls
            {
                {
                    step.jobStepId, new List<TheJobStepState>
                    {
                        step.jobStepState
                    }
                }
            },
            static (_, states, step) =>
            {
                if (!states.TryGetValue(step.jobStepId, out var jobStepStates))
                {
                    jobStepStates = [];
                }
                jobStepStates.Add(step.jobStepState);
                states[step.jobStepId] = jobStepStates;
                return states;
            },
            (jobStepId, jobStepState));

        Interlocked.Increment(ref _numJobStepsStarted);
        if (_numJobStepsStarted >= _numJobStepsPrepared)
        {
            _allPreparedJobsStarted.TrySetResult();
        }
    }

    public void JobStepCompleted(JobId jobId, JobStepId jobStepId, TheJobStepState jobStepState, JobStepStatus status)
    {
        CompletedSteps.AddOrUpdate(
            jobId,
            static (_, step) => new CompletedJobSteps
            {
                {
                    step.jobStepId, (step.jobStepState, step.status)
                }
            },
            static (_, states, step) =>
            {
                states[step.jobStepId] = (step.jobStepState, step.status);
                return states;
            },
            (jobStepId, jobStepState, status));
        Interlocked.Increment(ref _numJobStepsCompleted);
        if (_numJobStepsCompleted >= _numJobStepsToComplete)
        {
            _jobsCompleted.TrySetResult();
        }
    }
}
