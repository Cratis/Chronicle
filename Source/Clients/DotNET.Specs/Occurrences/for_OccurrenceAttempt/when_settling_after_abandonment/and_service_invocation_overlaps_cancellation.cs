// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences.given;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_settling_after_abandonment;

public class and_service_invocation_overlaps_cancellation : Specification
{
    bool _callbackRanBeforeInvocationReturned;
    bool _readmission;
    bool _released;

    async Task Because()
    {
        var clock = new ManualOccurrenceClock();
        using var session = new CancellationTokenSource();
        await using var lifetime = new OccurrenceAttemptLifetime(clock, clock.GetTimestamp(), session.Token);
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var registration = lifetime.Token.Register(() =>
        {
            _readmission = lifetime.Admit(settlement: true) is not null;
            callback.TrySetResult();
        });
        var ticket = lifetime.Admit(settlement: true);
        var invocation = Task.Run(() => ticket.Invoke(_ =>
        {
            entered.TrySetResult();

            // Deliberately hold the synchronous external invocation, not a task continuation.
            _released = release.Wait(TimeSpan.FromSeconds(5));
            return Task.FromResult(true);
        }));
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            await session.CancelAsync().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            await callback.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _callbackRanBeforeInvocationReturned = !invocation.IsCompleted;
        }
        finally
        {
            release.Set();
            await invocation.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
    }

    [Fact] void should_allow_reentrant_cancellation_while_external_work_is_in_progress() => _callbackRanBeforeInvocationReturned.ShouldBeTrue();
    [Fact] void should_not_readmit_from_the_cancellation_callback() => _readmission.ShouldBeFalse();
    [Fact] void should_release_the_synchronous_invocation() => _released.ShouldBeTrue();
}
