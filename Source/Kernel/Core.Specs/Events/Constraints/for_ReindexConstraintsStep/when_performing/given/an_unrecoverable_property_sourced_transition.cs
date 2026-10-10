// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing.given;

public abstract class an_unrecoverable_property_sourced_transition : for_ReindexConstraintsStep.given.a_closing_constraint_reindex_step
{
    protected ClosedStream _existing;
    protected abstract EventTypeId RedactedType { get; }

    async Task Establish()
    {
        _definitions.Add(new ClosesStreamConstraintDefinition("other", ["OtherClosed"], ClosedStreamDimensions.EventSourceId, []));
        _state.Changes.Add(new("other", true, [ConstraintChangeType.EventAdded]));
        _existing = new(new(EventSourceId: "source", EventStreamId: "May"), "closing", new EventSequenceNumber(10), null);
        await _rows.Close(_existing);
        await _rows.Close(new(new(EventSourceId: "stale"), "other", EventSequenceNumber.First, null));
        _events.Add(Event("Closed", 5, Payload("period", "April")));
        _events.Add(Event(GlobalEventTypes.Redaction, 6, Payload("originalEventType", RedactedType.Value)));
        _events.Add(Event("OtherClosed", 7));
    }
}
