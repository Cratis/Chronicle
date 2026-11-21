// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ShutdownDrainBudgetExtensions.when_waiting_within_the_deadline;

/// <summary>
/// The deadline cancels the wait, the call then completes successfully, and only then does the waiting code resume.
/// The call's real outcome is kept: it completed, so it is not reported as abandoned and no cancellation escapes.
/// </summary>
public class and_the_call_completes_after_the_deadline_cancels_the_wait : Specification
{
    readonly CancellationTokenSource _deadline = new();
    readonly TaskCompletionSource _call = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly QueuedSynchronizationContext _context = new();
    bool _completed;

    async Task Because()
    {
        Task<bool> waiting = null!;
        _context.Run(() => waiting = _call.Task.CompletesBefore(_deadline.Token));
        await _deadline.CancelAsync();
        _call.SetResult();
        _context.ResumeAll();
        _completed = await waiting.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_report_the_call_as_completed() => _completed.ShouldBeTrue();
}
