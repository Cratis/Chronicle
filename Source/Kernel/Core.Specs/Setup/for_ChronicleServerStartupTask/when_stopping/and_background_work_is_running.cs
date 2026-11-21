// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_stopping;

/// <summary>
/// Shutdown during the background work stops it and never waits for a call that does not answer.
/// </summary>
public class and_background_work_is_running : given.a_startup_task_with_persisted_capture
{
    readonly TaskCompletionSource _never = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ILifecycleObserver _observer;
    Exception _error;

    void Establish()
    {
        _reactorObserver.Ensure().Returns(_ => _never.Task);
        var lifecycle = Substitute.For<ISiloLifecycle>();
        lifecycle.When(_ => _.Subscribe(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<ILifecycleObserver>()))
            .Do(call => _observer = call.Arg<ILifecycleObserver>());
        _startupTask.Participate(lifecycle);
    }

    async Task Because()
    {
        await ExecuteStartup();
        _error = await Catch.Exception(() => _observer.OnStop(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(30)));
    }

    void Destroy() => _never.TrySetResult();

    [Fact] void should_stop_without_waiting_for_the_pending_call() => _error.ShouldBeNull();
    [Fact] void should_have_ended_the_background_work() => _startupTask.DeferredWork.IsCompleted.ShouldBeTrue();
    [Fact] void should_report_it_stopped() => LogEntries.Any(_ => _.Event == nameof(ChronicleServerStartupTaskLogging.DeferredStartupWorkStopped)).ShouldBeTrue();
    [Fact] void should_not_report_it_finished() => LogEntries.Any(_ => _.Event == nameof(ChronicleServerStartupTaskLogging.DeferredStartupWorkFinished)).ShouldBeFalse();
    [Fact] void should_not_report_giving_up() => LogEntries.Any(_ => _.Event == nameof(ChronicleServerStartupTaskLogging.DeferredStartupWorkGaveUp)).ShouldBeFalse();
}
