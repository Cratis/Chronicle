// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_the_keys_exceed_the_cap : given.a_replay
{
    void Establish()
    {
        for (ulong index = 0; index < 12; index++) Event(index, $"source-{index}", "Claimed", $"value-{index}");
        Start(cap: 4);
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_use_more_than_one_segment() => _replay.SegmentCount.ShouldBeGreaterThan(1);
    [Fact] void should_write_all_final_claims() => _writes.Count.ShouldEqual(12);
    [Fact] async Task should_keep_the_last_source_value() => (await Values("source-11")).ShouldContainOnly(Hash("value-11").Value);
    [Fact] void should_never_exceed_the_combined_key_cap() => _replay.MaxKeysHeld.ShouldBeLessThanOrEqual(4);
    [Fact] void should_complete_all_segments() => _result.IsCompleted.ShouldBeTrue();
}
