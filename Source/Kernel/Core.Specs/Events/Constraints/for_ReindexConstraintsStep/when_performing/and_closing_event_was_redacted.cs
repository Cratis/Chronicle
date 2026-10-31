// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_closing_event_was_redacted : for_ReindexConstraintsStep.given.a_closing_constraint_reindex_step
{
    ClosedStream _snapshot;

    async Task Establish()
    {
        _snapshot = new(new(EventSourceId: "source", EventStreamId: "April"), "closing", new EventSequenceNumber(5), new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        await _rows.Close(_snapshot);
        _events.Add(Event(GlobalEventTypes.Redaction, 5, Payload("originalEventType", "Closed")));
    }

    async Task Because() => await Perform();

    [Fact] void should_complete_without_an_exception() => _result.TryGetException(out _).ShouldBeFalse();
    [Fact] async Task should_preserve_the_original_scope_and_metadata() => (await _rows.GetForOwner("closing")).ShouldContainOnly(_snapshot);
}
