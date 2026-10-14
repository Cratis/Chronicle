// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_close_was_missed : given.a_replay
{
    void Establish()
    {
        Event(0, "source", "Closed");
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] async Task should_close_the_scope() => (await _closed.GetForOwner("closing")).Single().Scope.EventSourceId!.Value.ShouldEqual("source");
    [Fact] async Task should_stamp_the_events_occurrence_time() => (await _closed.GetForOwner("closing")).Single().ClosedAt.ShouldEqual(_events.Single().Context.Occurred);
}
