// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute.Extensions;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_an_attempt_is_abandoned : given.a_replay
{
    TaskCompletionSource _hung;
    ConstraintIndexReplayResult _resumed;
    int _laterWritesBeforeResuming;

    void Establish()
    {
        Event(0, "first", "Claimed", "first");
        Event(1, "second", "Claimed", "second");
        _hung = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _unique.Configure().Save("first", _definition, 0, Hash("first"), string.Empty, Arg.Any<CancellationToken>()).Returns(_hung.Task);
        Start();
    }

    async Task Because()
    {
        _result = await _replay.Continue();
        _laterWritesBeforeResuming = _writes.Count;
        _unique.Configure().Save("first", _definition, 0, Hash("first"), string.Empty, Arg.Any<CancellationToken>()).Returns(_ => _claims.Save("first", _definition, 0, Hash("first")));
        _resumed = await _replay.Continue();
    }

    void Destroy() => _hung.TrySetResult();

    [Fact] void should_report_the_abandoned_constraint() => _result.Abandoned.Select(name => name.Value).ShouldContainOnly("unique");
    [Fact] void should_not_report_abandonment_as_failure() => _result.Failed.ShouldBeEmpty();
    [Fact] void should_stop_later_writes_in_the_same_turn() => _laterWritesBeforeResuming.ShouldEqual(0);
    [Fact] void should_keep_the_first_turn_pending() => _result.IsCompleted.ShouldBeFalse();
    [Fact] void should_resume_the_remaining_writes() => _resumed.IsCompleted.ShouldBeTrue();
    [Fact] void should_not_carry_abandonment_into_the_next_result() => _resumed.Abandoned.ShouldBeEmpty();
    [Fact] async Task should_apply_the_later_claim_in_the_next_turn() => (await Values("second")).ShouldContainOnly(Hash("second").Value);
}
