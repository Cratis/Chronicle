// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.for_EventSequence.given;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexUpdateAttempt;

public class when_the_configured_attempt_timeout_elapses : Specification
{
    readonly RepairClock _clock = new();
    readonly TaskCompletionSource _write = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task<(ConstraintIndexUpdateOutcome Outcome, Exception? Error)> _run;
    bool _completedAtTheFormerDefault;
    ConstraintIndexUpdateOutcome _outcome;

    async Task Because()
    {
        _run = ConstraintIndexUpdateAttempt.Run(_ => _write.Task, TimeSpan.FromSeconds(3), CancellationToken.None, _clock);
        _clock.Advance(TimeSpan.FromSeconds(1));
        _completedAtTheFormerDefault = _run.IsCompleted;
        _clock.Advance(TimeSpan.FromSeconds(2));
        (_outcome, _) = await _run;
    }

    void Destroy() => _write.TrySetResult();

    [Fact] void should_not_give_up_at_the_former_default() => _completedAtTheFormerDefault.ShouldBeFalse();
    [Fact] void should_abandon_the_write_after_the_configured_timeout() => _outcome.ShouldEqual(ConstraintIndexUpdateOutcome.Abandoned);
}
