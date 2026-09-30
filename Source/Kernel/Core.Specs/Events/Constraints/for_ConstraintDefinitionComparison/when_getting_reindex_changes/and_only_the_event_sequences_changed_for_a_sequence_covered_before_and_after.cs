// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintDefinitionComparison.when_getting_reindex_changes;

/// <summary>
/// Narrowing a constraint from every event sequence to the event log changes nothing about how the event log is
/// indexed, so the event log's index is left alone.
/// </summary>
public class and_only_the_event_sequences_changed_for_a_sequence_covered_before_and_after : Specification
{
    UniqueConstraintDefinition _previous;
    UniqueConstraintDefinition _current;
    IReadOnlyCollection<ConstraintDefinitionChange> _reindexChanges;

    void Establish()
    {
        _previous = new UniqueConstraintDefinition(
            "some-unique",
            [new UniqueConstraintEventDefinition("some-event", ["Some"])]);
        _current = _previous with { EventSequences = [EventSequenceId.Log] };
    }

    void Because() => _reindexChanges = ConstraintDefinitionComparison.GetReindexChanges([_previous], [_current], EventSequenceId.Log);

    [Fact] void should_not_require_a_reindex() => _reindexChanges.ShouldBeEmpty();
}
