// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute.Extensions;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_collecting_resumes_after_a_budget : given.a_replay
{
    CancellationTokenSource _budget;
    ConstraintIndexReplayResult _resumed;

    void Establish()
    {
        Event(0, "first", "Claimed", "one");
        Event(1, "second", "Claimed", "two");
        _budget = new();
        _unique.Configure().GetFor("second", _definition, string.Empty, Arg.Any<CancellationToken>()).Returns(async call =>
        {
            await _budget.CancelAsync();
            return await _claims.GetFor("second", _definition);
        });
        Start();
    }

    async Task Because()
    {
        _result = await _replay.Continue(cancellationToken: _budget.Token);
        _unique.Configure().GetFor("second", _definition, string.Empty, Arg.Any<CancellationToken>()).Returns(_ => _claims.GetFor("second", _definition));
        _resumed = await _replay.Continue();
    }

    void Destroy() => _budget.Dispose();

    [Fact] void should_pause_without_failure() => _result.Failed.ShouldBeEmpty();
    [Fact] void should_resume_to_completion() => _resumed.IsCompleted.ShouldBeTrue();
    [Fact] void should_write_each_claim_only_once() => _writes.Count.ShouldEqual(2);
}
