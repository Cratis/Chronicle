// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_catch_up_retry_follows_a_boundary_raise : given.a_replay
{
    async Task Establish()
    {
        var missing = new HashSet<string> { "s1", "s3", "s5", "s6", "s8", "s12" };
        await _claims.Save("s0", _definition, 0, Hash("transfer"));
        for (var index = 0; index < 32; index++)
        {
            var source = $"s{index}";
            var value = source == "s1" ? "transfer" : source;
            Event((ulong)index, source, "Claimed", value);
            if (!missing.Contains(source) && source != "s0") await _claims.Save(source, _definition, (ulong)index, Hash(value));
        }
        _replay = new(_sequence, _unique, _closed, _content, Microsoft.Extensions.Logging.Abstractions.NullLogger<ConstraintIndexReplay>.Instance, [new(_definition, EventSequenceNumber.First)], 32, 12)
        {
            CatchUp = new(_ => false, _ => false, (_, _) => Task.CompletedTask, _ => Task.CompletedTask)
        };
        for (var turn = 0; turn < 100 && _replay.SegmentCount < 4; turn++) await _replay.Continue(1);
        _replay.RaiseCatchUpBoundary(33);
    }

    async Task Because() => _result = await _replay.Continue(int.MaxValue);

    [Fact] void should_exercise_four_segments() => _replay.SegmentCount.ShouldEqual(4);
    [Fact] void should_complete_the_retry_pass() => _result.IsCompleted.ShouldBeTrue();
    [Fact] async Task should_retry_the_claim_in_segment_one_after_segment_three_releases() => (await Values("s1")).ShouldContainOnly(Hash("transfer").Value);
    [Fact] void should_attempt_the_claim_again() => _writes.Count(write => write == $"claim:s1:{Hash("transfer")}").ShouldEqual(2);
}
