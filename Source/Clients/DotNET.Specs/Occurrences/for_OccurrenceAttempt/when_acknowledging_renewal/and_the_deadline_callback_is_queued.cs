// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences.given;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_acknowledging_renewal;

public class and_the_deadline_callback_is_queued : Specification
{
    readonly ManualOccurrenceClock _clock = new();
    bool _activeAfterTheStaleCallback;
    bool _expiredAtTheNewDeadline;

    async Task Because()
    {
        await using var lifetime = new OccurrenceAttemptLifetime(_clock, _clock.GetTimestamp(), CancellationToken.None);
        _clock.Advance(TimeSpan.FromSeconds(20));
        lifetime.Accept(lifetime.Admit());
        _clock.Advance(TimeSpan.FromSeconds(30));
        var renewal = lifetime.Admit();
        _clock.Advance(TimeSpan.FromSeconds(10), deliverCallbacks: false);
        var queued = _clock.CaptureDueCallbacks();
        lifetime.Accept(renewal);
        foreach (var callback in queued) callback();
        await _clock.WaitForTimer(TimeSpan.FromSeconds(50));
        _activeAfterTheStaleCallback = !lifetime.Token.IsCancellationRequested;
        var expired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var registration = lifetime.Token.Register(() => expired.TrySetResult());
        _clock.Advance(TimeSpan.FromSeconds(50));
        await expired.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _expiredAtTheNewDeadline = lifetime.Admit() is null;
    }

    [Fact] void should_recheck_the_current_anchor_after_a_queued_callback() => _activeAfterTheStaleCallback.ShouldBeTrue();
    [Fact] void should_still_expire_at_the_new_deadline() => _expiredAtTheNewDeadline.ShouldBeTrue();
}
