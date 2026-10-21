// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_move_was_missed : given.a_replay
{
    async Task Establish()
    {
        await _claims.Save("source", _definition, 0, Hash("old"));
        Event(1, "source", "Claimed", "new");
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_release_then_claim() => _writes.ShouldEqual(new[] { "release:source", $"claim:source:{Hash("new")}" });
    [Fact] async Task should_hold_only_the_new_value() => (await Values("source")).ShouldContainOnly(Hash("new").Value);
}
