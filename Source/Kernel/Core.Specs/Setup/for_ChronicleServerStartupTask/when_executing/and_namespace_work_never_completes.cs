// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// Per-namespace work calls grains that may live on a sibling silo that is too busy to answer. Readiness must not wait
/// for it: startup completes while the background work is still pending.
/// </summary>
public class and_namespace_work_never_completes : given.a_startup_task_with_persisted_capture
{
    readonly TaskCompletionSource _never = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception _error;

    void Establish() => _reactorObserver.Ensure().Returns(_ => _never.Task);

    async Task Because() => _error = await Catch.Exception(() => ExecuteStartup().WaitAsync(TimeSpan.FromSeconds(30)));

    void Destroy() => _never.TrySetResult();

    [Fact] void should_complete_startup() => _error.ShouldBeNull();
    [Fact] void should_run_the_final_authentication_step() => _bootstrapClientsEnsured.ShouldBeTrue();
    [Fact] void should_report_startup_completed() => LogEntries.Any(_ => _.Event == nameof(ChronicleServerStartupTaskLogging.StartupCompleted)).ShouldBeTrue();
    [Fact] void should_report_every_startup_phase() => LogEntries.Count(_ => _.Event == nameof(ChronicleServerStartupTaskLogging.StartupPhaseCompleted)).ShouldEqual(4);
    [Fact] void should_still_be_working_in_the_background() => _startupTask.DeferredWork.IsCompleted.ShouldBeFalse();
    [Fact] void should_not_have_reported_the_background_work_finished() => LogEntries.Any(_ => _.Event == nameof(ChronicleServerStartupTaskLogging.DeferredStartupWorkFinished)).ShouldBeFalse();
}
