// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute.Extensions;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_the_budget_expires_during_a_write : given.a_replay
{
    CancellationTokenSource _budget;
    TaskCompletionSource _hung;
    ConstraintIndexReplayResult _resumed;

    void Establish()
    {
        Event(0, "first", "Claimed", "first");
        Event(1, "second", "Claimed", "second");
        _budget = new();
        _hung = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _unique.Configure().Save("second", _definition, 1, Hash("second"), string.Empty, Arg.Any<Subject?>(), Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            await _budget.CancelAsync();
            await _hung.Task;
        });
        Start();
    }

    async Task Because()
    {
        _result = await _replay.Continue(cancellationToken: _budget.Token);
        _unique.Configure().Save("second", _definition, 1, Hash("second"), string.Empty, Arg.Any<Subject?>(), Arg.Any<CancellationToken>()).Returns(_ => _claims.Save("second", _definition, 1, Hash("second")));
        _resumed = await _replay.Continue();
    }

    void Destroy()
    {
        _hung.TrySetResult();
        _budget.Dispose();
    }

    [Fact] void should_pause_without_a_storage_failure() => _result.Failed.ShouldBeEmpty();
    [Fact] void should_not_report_the_first_turn_complete() => _result.IsCompleted.ShouldBeFalse();
    [Fact] void should_resume_the_remaining_writes() => _resumed.IsCompleted.ShouldBeTrue();
    [Fact] async Task should_preserve_the_confirmed_first_claim() => (await Values("first")).ShouldContainOnly(Hash("first").Value);
}
