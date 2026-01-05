// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Grains.Jobs.for_JobStepThrottle;

public class when_acquiring_and_releasing_slots : Specification
{
    JobStepThrottle _throttle;
    ChronicleOptions _options;
    List<Task> _tasks;
    int _completedTasks;

    void Establish()
    {
        _options = new ChronicleOptions
        {
            Jobs = new Configuration.Jobs { MaxParallelSteps = 2 }
        };
        _throttle = new JobStepThrottle(Options.Create(_options), NullLogger<JobStepThrottle>.Instance);
        _tasks = [];
        _completedTasks = 0;
    }

    async Task Because()
    {
        // Start 4 tasks but only 2 should run in parallel
        for (var i = 0; i < 4; i++)
        {
            var task = Task.Run(async () =>
            {
                await _throttle.AcquireAsync();
                try
                {
                    Interlocked.Increment(ref _completedTasks);
                    await Task.Delay(50); // Simulate work
                }
                finally
                {
                    _throttle.Release();
                }
            });
            _tasks.Add(task);
        }

        await Task.WhenAll(_tasks);
    }

    [Fact] void should_complete_all_tasks() => _completedTasks.ShouldEqual(4);

    void Cleanup() => _throttle.Dispose();
}
