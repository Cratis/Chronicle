// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_closing_constraint_changed : for_ReindexConstraintsStep.given.a_closing_constraint_reindex_step
{
    async Task Establish()
    {
        await _rows.Close(new(new(EventSourceId: "stale"), "closing", EventSequenceNumber.First, null));
        await _rows.Close(new(new(EventSourceId: "manual"), ClosedStreamOwner.Manual, EventSequenceNumber.First, null));
        _events.Add(Event("Closed", 5, Payload("period", "April")));
    }

    async Task Because() => await Perform();

    [Fact] void should_complete_without_an_exception() => _result.TryGetException(out _).ShouldBeFalse();
    [Fact] async Task should_rebuild_the_declared_payload_scope() => (await _rows.GetForOwner("closing")).Single().Scope.ShouldEqual(new ClosedStreamScope(EventSourceId: "source", EventStreamId: "April"));
    [Fact] async Task should_record_the_original_event_position() => (await _rows.GetForOwner("closing")).Single().SequenceNumber.ShouldEqual(new EventSequenceNumber(5));
    [Fact] async Task should_keep_manual_closures() => (await _rows.GetForOwner(ClosedStreamOwner.Manual)).Count().ShouldEqual(1);
}
