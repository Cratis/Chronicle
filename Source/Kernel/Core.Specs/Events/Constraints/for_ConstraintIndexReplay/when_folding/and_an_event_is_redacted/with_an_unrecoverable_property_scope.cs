// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding.and_an_event_is_redacted;

public class with_an_unrecoverable_property_scope : given.a_replay
{
    void Establish()
    {
        _closing = _closing with { Dimensions = ClosedStreamDimensions.EventStreamId, EventStreamIdFrom = "stream" };
        Event(0, "source", GlobalEventTypes.Redaction.Value);
        ((IDictionary<string, object?>)_events[0].Content)["originalEventType"] = "Closed";
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_settle_with_error_instead_of_reconstructing_a_scope() => _result.SettledWithError.ShouldEqual(1);
    [Fact] async Task should_write_no_closure() => (await _closed.GetForOwner("closing")).ShouldBeEmpty();
}
