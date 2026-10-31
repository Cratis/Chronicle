// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_the_store_reports_a_duplicate_claim : given.a_replay
{
    async Task Establish()
    {
        await _claims.Save("holder", _definition, 0, Hash("held"));
        await _claims.Save("source", _definition, 1, Hash("old"));
        Event(2, "source", "Claimed", "held");
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_count_the_settled_refusal() => _result.Settled.ShouldEqual(1);
    [Fact] async Task should_leave_the_refused_source_without_its_old_entry() => (await Values("source")).ShouldBeEmpty();
    [Fact] async Task should_preserve_the_holder() => (await Values("holder")).ShouldContainOnly(Hash("held").Value);
    [Fact] void should_not_retry_an_unchanged_holder() => _writes.Count.ShouldEqual(2);
}
