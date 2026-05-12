// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Jobs.for_JobStepThrottle;

public class when_observer_partition_limit_is_lower_than_job_step_limit : Specification
{
    JobStepThrottle _throttle;
    ChronicleOptions _options;
    int _completedTasks;

    void Establish()
    {
        _options = new ChronicleOptions
        {
            Jobs = new Configuration.Jobs { MaxParallelSteps = 8 },
            Observers = new Configuration.Observers { MaxConcurrentPartitions = 1 }
        };
        _throttle = new JobStepThrottle(Options.Create(_options), NullLogger<JobStepThrottle>.Instance);
        _completedTasks = 0;
    }

    async Task Because()
    {
        var firstTaskStarted = new TaskCompletionSource<bool>();
        var releaseFirstTask = new TaskCompletionSource<bool>();
        var secondTaskEntered = false;

        var firstTask = Task.Run(async () =>
        {
            await _throttle.AcquireAsync();
            Interlocked.Increment(ref _completedTasks);
            firstTaskStarted.SetResult(true);
            await releaseFirstTask.Task;
            _throttle.Release();
        });

        await firstTaskStarted.Task;

        var secondTask = Task.Run(async () =>
        {
            await _throttle.AcquireAsync();
            secondTaskEntered = true;
            Interlocked.Increment(ref _completedTasks);
            _throttle.Release();
        });

        await Task.Delay(50);
        secondTaskEntered.ShouldBeFalse();
        releaseFirstTask.SetResult(true);

        await Task.WhenAll(firstTask, secondTask);
        secondTaskEntered.ShouldBeTrue();
    }

    [Fact] void should_apply_observer_partition_limit() => _completedTasks.ShouldEqual(2);

    void Cleanup() => _throttle.Dispose();
}
