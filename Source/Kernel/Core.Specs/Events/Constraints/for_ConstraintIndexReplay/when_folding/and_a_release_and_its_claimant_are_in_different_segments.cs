// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_release_and_its_claimant_are_in_different_segments : given.a_replay
{
    async Task Establish()
    {
        // SHA-256("unique\\0\\0a\\0") is even and SHA-256("unique\\0\\0f\\0") is odd:
        // at two segments the claimant is folded before the releasing source.
        await _claims.Save("f", _definition, 0, Hash("v"));
        Event(1, "a", "Claimed", "v");
        Event(2, "f", "Claimed", "w");
        ulong number = 3;
        foreach (var source in new[] { "b", "holder", "y", "x", "c", "d", "e", "claimant" })
        {
            Event(number, source, "Claimed", source);
            await _claims.Save(source, _definition, number, Hash(source));
            number++;
        }
        Start(cap: 8);
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_use_separate_even_and_odd_segments() => _replay.SegmentCount.ShouldEqual(2);
    [Fact] void should_release_before_any_claim_across_segments() => _writes[0].ShouldEqual("release:f");
    [Fact] async Task should_protect_the_transferred_value() => (await Values("a")).ShouldContainOnly(Hash("v").Value);
    [Fact] void should_settle_no_claim_as_duplicate() => _result.Settled.ShouldEqual(0);
}
