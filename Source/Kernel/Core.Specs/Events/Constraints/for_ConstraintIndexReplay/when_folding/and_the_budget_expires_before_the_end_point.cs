// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_the_budget_expires_before_the_end_point : given.a_replay
{
    async Task Establish()
    {
        Event(0, "source", "Claimed", "earlier");
        Event(1, "source", "Claimed", "newer");
        await _claims.Save("source", _definition, 1, Hash("newer"));
        Start();
    }

    async Task Because() => _result = await _replay.Continue(eventLimit: 1);

    [Fact] void should_write_nothing() => _writes.ShouldBeEmpty();
    [Fact] void should_keep_the_fold_pending() => _result.IsCompleted.ShouldBeFalse();
    [Fact] async Task should_keep_the_newer_entry() => (await Values("source")).ShouldContainOnly(Hash("newer").Value);
}
