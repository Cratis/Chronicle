// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_closing_scope_cannot_be_resolved : given.a_replay
{
    void Establish()
    {
        _closing = _closing with { Dimensions = ClosedStreamDimensions.EventStreamId, EventStreamIdFrom = "missing" };
        Event(0, "source", "Closed");
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_settle_with_error() => _result.SettledWithError.ShouldEqual(1);
    [Fact] void should_complete_the_range() => _result.IsCompleted.ShouldBeTrue();
    [Fact] async Task should_write_no_closure() => (await _closed.GetForOwner("closing")).ShouldBeEmpty();
}
