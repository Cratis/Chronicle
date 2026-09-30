// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_UniqueConstraintDefinition.when_comparing_with_a_definition_applying_to_other_event_sequences;

/// <summary>
/// The index of a newly covered event sequence was never maintained, so widening the event sequences is a change
/// that requires a reindex - and one registration must persist, or the stored definition keeps the old scope.
/// </summary>
public class and_it_now_applies_to_more_event_sequences : Specification
{
    static readonly ConstraintName _name = "some-constraint";

    UniqueConstraintDefinition _existing;
    UniqueConstraintDefinition _definition;
    ConstraintChange _change;

    void Establish()
    {
        _existing = new(_name, [new UniqueConstraintEventDefinition("the-event-type", ["the-property"])])
        {
            EventSequences = [EventSequenceId.Log]
        };
        _definition = _existing with { EventSequences = [EventSequenceId.Log, EventSequenceId.Outbox] };
    }

    void Because() => _change = _definition.CompareWith(_existing);

    [Fact] void should_not_be_equal() => _definition.Equals(_existing).ShouldBeFalse();
    [Fact] void should_require_reindex() => _change.RequiresReindex.ShouldBeTrue();
    [Fact] void should_report_the_event_sequences_as_changed() => _change.ChangeTypes.ShouldContainOnly([ConstraintChangeType.EventSequencesChanged]);
}
