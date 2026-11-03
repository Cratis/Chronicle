// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// A restart meets the backlog it left behind. Running startup again while an observer is still activating must
/// complete without blocking, and must not start a second overlapping activation of the same observer.
/// </summary>
public class and_the_backlog_is_still_pending_when_startup_runs_again : given.a_startup_task_with_a_persisted_reactor
{
    readonly TaskCompletionSource _neverActivates = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception _error;

    void Establish() => _reactorObserver.Ensure().Returns(_ => _neverActivates.Task);

    async Task Because()
    {
        await Execute().WaitAsync(TimeSpan.FromSeconds(5));
        _error = await Catch.Exception(() => Execute().WaitAsync(TimeSpan.FromSeconds(5)));
        await Task.Delay(100);
    }

    void Destroy()
    {
        _neverActivates.TrySetResult();
        Stop().GetAwaiter().GetResult();
    }

    [Fact] void should_not_block_the_second_startup() => _error.ShouldBeNull();
    [Fact] async Task should_have_one_activation_in_flight_for_the_observer() => await _reactorObserver.Received(1).Ensure();
}
