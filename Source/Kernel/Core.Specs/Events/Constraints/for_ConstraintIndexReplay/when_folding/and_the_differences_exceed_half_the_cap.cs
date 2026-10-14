// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_the_differences_exceed_half_the_cap : given.a_replay
{
    async Task Establish()
    {
        await _claims.Save("f", _definition, 0, Hash("v"));
        Event(1, "a", "Claimed", "v");
        Event(2, "f", "Claimed", "w");
        ulong number = 3;
        foreach (var source in new[] { "b", "holder", "y", "x", "c", "d", "e", "claimant" }) Event(number++, source, "Claimed", source);
        Start(cap: 8);
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_write_a_chunk_before_remaining_passes_finish() => _readsAtWrites[0].ShouldBeLessThan(_sequence.ReceivedCalls().Count());
    [Fact] void should_retry_the_deferred_claim_after_remaining_releases() => _writes.Count(write => write == $"claim:a:{Hash("v")}").ShouldEqual(2);
    [Fact] async Task should_give_the_value_to_its_final_holder() => (await Values("a")).ShouldContainOnly(Hash("v").Value);
    [Fact] async Task should_preserve_the_releasing_sources_final_claim() => (await Values("f")).ShouldContainOnly(Hash("w").Value);
    [Fact] void should_not_settle_the_initial_refusal() => _result.Settled.ShouldEqual(0);
}
