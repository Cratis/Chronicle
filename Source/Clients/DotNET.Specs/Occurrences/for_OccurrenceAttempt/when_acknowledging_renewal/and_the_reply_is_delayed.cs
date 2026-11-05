// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences.given;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceAttempt.when_acknowledging_renewal;

public class and_the_reply_is_delayed : Specification
{
    readonly ManualOccurrenceClock _clock = new();
    bool _accepted;
    bool _activeAtTheOldDeadline;
    bool _expiredAtTheAdmissionDeadline;

    async Task Because()
    {
        await using var lifetime = new OccurrenceAttemptLifetime(_clock, _clock.GetTimestamp(), CancellationToken.None);
        _clock.Advance(TimeSpan.FromSeconds(20));
        var admission = lifetime.Admit();
        _clock.Advance(TimeSpan.FromSeconds(30));
        _accepted = lifetime.Accept(admission);
        _clock.Advance(TimeSpan.FromSeconds(10));
        await _clock.WaitForTimer(TimeSpan.FromSeconds(20));
        _activeAtTheOldDeadline = !lifetime.Token.IsCancellationRequested;
        var expired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var registration = lifetime.Token.Register(() => expired.TrySetResult());
        _clock.Advance(TimeSpan.FromSeconds(20));
        await expired.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _expiredAtTheAdmissionDeadline = lifetime.Admit() is null;
    }

    [Fact] void should_accept_a_timely_reply() => _accepted.ShouldBeTrue();
    [Fact] void should_not_expire_at_the_superseded_deadline() => _activeAtTheOldDeadline.ShouldBeTrue();
    [Fact] void should_anchor_at_admission_rather_than_reply_time() => _expiredAtTheAdmissionDeadline.ShouldBeTrue();
}
