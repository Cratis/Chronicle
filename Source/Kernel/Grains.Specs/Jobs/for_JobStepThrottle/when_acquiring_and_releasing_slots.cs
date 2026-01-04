// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Grains.Jobs.for_JobStepThrottle;

public class when_acquiring_and_releasing_slots : Specification
{
    JobStepThrottle throttle;
    ChronicleOptions options;
    List<Task> tasks;
    int completedTasks;

    void Establish()
    {
        options = new ChronicleOptions
        {
            Jobs = new Configuration.Jobs { MaxParallelSteps = 2 }
        };
        throttle = new JobStepThrottle(Options.Create(options), NullLogger<JobStepThrottle>.Instance);
        tasks = [];
        completedTasks = 0;
    }

    async Task Because()
    {
        // Start 4 tasks but only 2 should run in parallel
        for (var i = 0; i < 4; i++)
        {
            var task = Task.Run(async () =>
            {
                await throttle.AcquireAsync();
                try
                {
                    Interlocked.Increment(ref completedTasks);
                    await Task.Delay(50); // Simulate work
                }
                finally
                {
                    throttle.Release();
                }
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    [Fact] void should_complete_all_tasks() => completedTasks.ShouldEqual(4);

    void Cleanup() => throttle.Dispose();
}
