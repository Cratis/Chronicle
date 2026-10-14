// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_closing_event_precedes_the_coverage_start : given.a_replay
{
    void Establish()
    {
        Event(0, "source", "Closed");
        Event(1, "new-source", "Closed");
        Start(closingStart: 1);
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] async Task should_apply_only_the_covered_closing_event() => (await _closed.GetForOwner("closing")).Select(row => row.Scope.EventSourceId!.Value).ShouldContainOnly("new-source");
}
