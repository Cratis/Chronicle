// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_no_governed_event_types_exist : for_ReindexConstraintsStep.given.a_closing_constraint_reindex_step
{
    UniqueConstraintDefinition _unique;

    void Establish()
    {
        _unique = new("empty", []);
        _definitions.Add(_unique);
        _state.Changes = [new("empty", true, [ConstraintChangeType.EventAdded])];
        _events.Add(Event("Unrelated", 4));
    }

    async Task Because() => await Perform();

    [Fact] void should_complete() => _result.TryGetException(out _).ShouldBeFalse();
    [Fact] async Task should_not_read_an_empty_filter() => await _eventsStorage.DidNotReceiveWithAnyArgs().GetFromSequenceNumber(EventSequenceNumber.First);
    [Fact] async Task should_clear_the_unscoped_index() => await _uniqueStorage.Received(1).ClearValues(_unique, "");
    [Fact] async Task should_report_unknown_progress() => await _sequence.Received(1).ConstraintIndexRebuilt(_runId, Arg.Any<IEnumerable<ConstraintName>>(), ConstraintIndexRebuildOutcome.ProgressUnknown);
}
