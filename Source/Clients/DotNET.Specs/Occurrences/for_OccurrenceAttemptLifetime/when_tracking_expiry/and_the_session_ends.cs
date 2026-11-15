// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences.given;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttemptLifetime.when_tracking_expiry;

public class and_the_session_ends : Specification
{
    bool _cancelled;
    bool _expired;

    async Task Because()
    {
        var clock = new ManualOccurrenceClock();
        using var session = new CancellationTokenSource();
        await using var lifetime = new OccurrenceAttemptLifetime(clock, clock.GetTimestamp(), session.Token);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var registration = lifetime.Token.Register(() => stopped.TrySetResult());
        await session.CancelAsync();
        await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        clock.Advance(TimeSpan.FromSeconds(60));
        _cancelled = lifetime.Token.IsCancellationRequested;
        _expired = lifetime.Expired;
    }

    [Fact] void should_cancel_the_attempt() => _cancelled.ShouldBeTrue();
    [Fact] void should_not_be_expired() => _expired.ShouldBeFalse();
}
