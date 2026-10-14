// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_the_releasing_source_moved_again_later_in_the_range : given.a_replay
{
    async Task Establish()
    {
        await _claims.Save("x", _definition, 0, Hash("v"));
        Event(5, "x", "Claimed", "c");
        Event(6, "y", "Claimed", "v");
        Event(9, "x", "Claimed", "a");
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_release_before_any_claim() => _writes[0].ShouldEqual("release:x");
    [Fact] async Task should_give_the_released_value_to_its_final_holder() => (await Values("y")).ShouldContainOnly(Hash("v").Value);
    [Fact] async Task should_give_the_releasing_source_its_final_value() => (await Values("x")).ShouldContainOnly(Hash("a").Value);
    [Fact] void should_settle_no_claim_as_duplicate() => _result.Settled.ShouldEqual(0);
}
