// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintDefinitionComparison.when_getting_reindex_changes;

/// <summary>
/// A sequence a constraint no longer applies to stops validating and indexing it, so its index is no longer read and
/// there is nothing to rebuild. A later widening rebuilds it.
/// </summary>
public class and_the_constraint_no_longer_applies_to_the_event_sequence : Specification
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

    void Because() => _reindexChanges = ConstraintDefinitionComparison.GetReindexChanges([_previous], [_current], EventSequenceId.Outbox);

    [Fact] void should_not_require_a_reindex() => _reindexChanges.ShouldBeEmpty();
}
