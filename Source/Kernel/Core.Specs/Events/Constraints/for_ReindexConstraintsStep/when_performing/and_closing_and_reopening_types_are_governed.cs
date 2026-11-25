// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_closing_and_reopening_types_are_governed : for_ReindexConstraintsStep.given.a_closing_constraint_reindex_step
{
    void Establish()
    {
        _events.Add(Event("Closed", 1, Payload("period", "April")));
        _events.Add(Event("Reopened", 2, Payload("period", "April")));
    }

    async Task Because() => await Perform();

    [Fact] async Task should_read_closing_reopening_and_redaction() => await _eventsStorage.Received(1).GetFromSequenceNumber(EventSequenceNumber.First, eventTypes: Arg.Is<IEnumerable<EventType>>(types => types.Select(type => type.Id).SequenceEqual(new EventTypeId[] { "Closed", "Reopened", GlobalEventTypes.Redaction })), cancellationToken: Arg.Any<CancellationToken>());
    [Fact] async Task should_leave_the_reopened_scope_open() => (await _rows.GetForOwner("closing")).ShouldBeEmpty();
}
