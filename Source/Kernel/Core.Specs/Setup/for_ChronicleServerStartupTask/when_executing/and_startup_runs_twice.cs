// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// The deferred work is idempotent with the other silo doing it, and a silo starts it once however often its
/// lifecycle stage runs: the work is not repeated.
/// </summary>
public class and_startup_runs_twice : given.a_startup_task_with_persisted_capture
{
    async Task Because()
    {
        await Execute();
        await Execute();
    }

    [Fact] async Task should_warm_the_observers_once() => await _reactorObserver.Received(1).Ensure();
    [Fact] async Task should_rehydrate_the_jobs_once() => await _jobsManager.Received(1).Rehydrate();
    [Fact] void should_report_the_summary_once() => LogEntries.Count(_ => _.Event == nameof(ChronicleServerStartupTaskLogging.DeferredStartupWorkFinished)).ShouldEqual(1);
}
