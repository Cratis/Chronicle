// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_source_moved_later_in_the_range : given.a_replay
{
    void Establish()
    {
        Event(0, "source", "Claimed", "earlier");
        Event(1, "source", "Claimed", "final");
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_never_write_the_intermediate_value() => _writes.ShouldContainOnly($"claim:source:{Hash("final")}");
    [Fact] async Task should_hold_the_final_value() => (await Values("source")).ShouldContainOnly(Hash("final").Value);
}
