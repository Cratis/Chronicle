// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ShutdownDrainBudgetExtensions.when_waiting_within_the_deadline;

/// <summary>
/// The deadline cancels the wait, the call then fails, and only then does the waiting code resume. The failure is the call's
/// real outcome and must surface rather than be hidden behind the cancellation.
/// </summary>
public class and_the_call_fails_after_the_deadline_cancels_the_wait : Specification
{
    readonly CancellationTokenSource _deadline = new();
    readonly TaskCompletionSource _call = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly QueuedSynchronizationContext _context = new();
    Exception _failure;

    async Task Because()
    {
        Task<bool> waiting = null!;
        _context.Run(() => waiting = _call.Task.CompletesBefore(_deadline.Token));
        await _deadline.CancelAsync();
        _call.SetException(new InvalidOperationException("The call failed"));
        _context.ResumeAll();
        try
        {
            await waiting.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
        catch (Exception ex)
        {
            _failure = ex;
        }
    }

    [Fact] void should_surface_the_failure_of_the_call() => _failure.ShouldBeOfExactType<InvalidOperationException>();
}
