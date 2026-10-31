// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_a_metadata_sourced_closing_event_was_redacted : for_ReindexConstraintsStep.given.a_closing_constraint_reindex_step
{
    void Establish()
    {
        _definitions[0] = _closing with { EventStreamIdFrom = null };
        _events.Add(Event(GlobalEventTypes.Redaction, 5, Payload("originalEventType", "Closed")));
    }

    async Task Because() => await Perform();

    [Fact] void should_complete_without_an_exception() => _result.TryGetException(out _).ShouldBeFalse();
    [Fact] async Task should_reconstruct_the_scope_from_preserved_context() => (await _rows.GetForOwner("closing")).Single().Scope.ShouldEqual(new ClosedStreamScope(EventSourceId: "source", EventStreamId: "month"));
}
