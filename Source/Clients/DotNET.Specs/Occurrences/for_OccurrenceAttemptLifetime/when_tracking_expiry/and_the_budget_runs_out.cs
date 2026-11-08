// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences.given;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttemptLifetime.when_tracking_expiry;

public class and_the_budget_runs_out : Specification
{
    bool _expired;
    bool _cancelled;

    async Task Because()
    {
        var clock = new ManualOccurrenceClock();
        await using var lifetime = new OccurrenceAttemptLifetime(clock, clock.GetTimestamp(), CancellationToken.None);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var registration = lifetime.Token.Register(() => stopped.TrySetResult());
        clock.Advance(TimeSpan.FromSeconds(60));
        await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _cancelled = lifetime.Token.IsCancellationRequested;
        _expired = lifetime.Expired;
    }

    [Fact] void should_cancel_the_attempt() => _cancelled.ShouldBeTrue();
    [Fact] void should_be_expired() => _expired.ShouldBeTrue();
}
