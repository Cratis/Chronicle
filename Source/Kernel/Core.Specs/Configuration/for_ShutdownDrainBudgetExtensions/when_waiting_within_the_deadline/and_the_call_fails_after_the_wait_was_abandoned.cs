// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ShutdownDrainBudgetExtensions.when_waiting_within_the_deadline;

/// <summary>
/// The wait is abandoned while the call still runs, and the call fails afterwards. The failure is observed, so it is never an
/// unobserved task exception.
/// </summary>
public class and_the_call_fails_after_the_wait_was_abandoned : Specification
{
    readonly CancellationTokenSource _deadline = new();
    readonly TaskCompletionSource _call = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task _task;
    bool _abandoned;

    async Task Because()
    {
        _task = _call.Task;
        var waiting = _task.CompletesBefore(_deadline.Token);
        await _deadline.CancelAsync();
        _abandoned = !await waiting.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _call.SetException(new InvalidOperationException("Too late"));
        await Task.WhenAny(_task);
    }

    [Fact] void should_have_abandoned_the_call() => _abandoned.ShouldBeTrue();
    [Fact] void should_have_faulted_the_call() => _task.IsFaulted.ShouldBeTrue();
}
