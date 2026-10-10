// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_the_closing_constraint_does_not_apply_to_the_sequence : for_ReindexConstraintsStep.given.a_closing_constraint_reindex_step
{
    ClosedStream _existing;

    async Task Establish()
    {
        _definitions[0] = _closing with { EventSequences = [EventSequenceId.Outbox] };
        _existing = new(new(EventSourceId: "source", EventStreamId: "May"), "closing", new EventSequenceNumber(10), null);
        await _rows.Close(_existing);
        _events.Add(Event("Closed", 5, Payload("period", "April")));
    }

    async Task Because() => await Perform();

    [Fact] void should_complete_without_an_exception() => _result.TryGetException(out _).ShouldBeFalse();
    [Fact] async Task should_not_modify_an_inapplicable_owners_rows() => (await _rows.GetForOwner("closing")).ShouldContainOnly(_existing);
    [Fact] async Task should_not_clear_the_inapplicable_owner() => await _closures.DidNotReceive().RemoveAllFor(new ClosedStreamOwner("closing"));
}
