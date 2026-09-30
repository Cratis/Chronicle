// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintDefinitionComparison.when_getting_reindex_changes;

/// <summary>
/// Widening a constraint to an event sequence it did not apply to leaves that sequence with an index nothing has
/// maintained - either never built, or stale since the constraint last stopped applying - so it must be rebuilt.
/// </summary>
public class and_the_constraint_now_applies_to_the_event_sequence : Specification
{
    UniqueConstraintDefinition _previous;
    UniqueConstraintDefinition _current;
    IReadOnlyCollection<ConstraintDefinitionChange> _reindexChanges;

    void Establish()
    {
        _previous = new UniqueConstraintDefinition(
            "some-unique",
            [new UniqueConstraintEventDefinition("some-event", ["Some"])])
        {
            EventSequences = [EventSequenceId.Log]
        };
        _current = _previous with { EventSequences = [] };
    }

    void Because() => _reindexChanges = ConstraintDefinitionComparison.GetReindexChanges([_previous], [_current], EventSequenceId.Outbox);

    [Fact] void should_derive_a_single_reindex_change() => _reindexChanges.Count.ShouldEqual(1);
    [Fact] void should_require_reindex() => _reindexChanges.First().RequiresReindex.ShouldBeTrue();
    [Fact] void should_report_the_event_sequences_as_changed() => _reindexChanges.First().ChangeTypes.ShouldContainOnly([ConstraintChangeType.EventSequencesChanged]);
}
