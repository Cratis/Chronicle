// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_an_unresolved_event_is_read_in_multiple_segments : given.a_replay
{
    void Establish()
    {
        _closing = _closing with { Dimensions = ClosedStreamDimensions.EventStreamId, EventStreamIdFrom = "missing" };
        Event(0, "source", "Closed");
        for (ulong index = 1; index < 12; index++) Event(index, $"source-{index}", "Claimed", $"value-{index}");
        Start(cap: 4);
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_use_multiple_segments() => _replay.SegmentCount.ShouldBeGreaterThan(1);
    [Fact] void should_count_the_unresolved_event_only_once() => _result.SettledWithError.ShouldEqual(1);
}
