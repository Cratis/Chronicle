// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexUpdateAttempt;

public class when_the_turn_is_cancelled_during_an_uncooperative_write : Specification
{
    readonly CancellationTokenSource _turn = new();
    readonly TaskCompletionSource _write = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ConstraintIndexUpdateOutcome _outcome;

    async Task Because()
    {
        (_outcome, _) = await ConstraintIndexUpdateAttempt.Run(
            async _ =>
            {
                await _turn.CancelAsync();
                await _write.Task;
            },
            _turn.Token);
    }

    void Destroy()
    {
        _write.TrySetResult();
        _turn.Dispose();
    }

    [Fact] void should_track_the_write_that_might_still_land() => _outcome.ShouldEqual(ConstraintIndexUpdateOutcome.Abandoned);
}
