// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintDefinitionComparison.when_getting_reindex_changes;

/// <summary>
/// A constraint is only indexed for the event sequences it applies to, so a sequence it does not apply to has no
/// index of it to rebuild.
/// </summary>
public class and_the_constraint_does_not_apply_to_the_event_sequence : Specification
{
    IConstraintDefinition _added;
    IReadOnlyCollection<ConstraintDefinitionChange> _reindexChanges;

    void Establish() => _added = new UniqueConstraintDefinition(
        "new-unique",
        [new UniqueConstraintEventDefinition("some-event", ["Some"])])
    {
        EventSequences = [EventSequenceId.Log]
    };

    void Because() => _reindexChanges = ConstraintDefinitionComparison.GetReindexChanges([], [_added], EventSequenceId.Outbox);

    [Fact] void should_not_require_a_reindex() => _reindexChanges.ShouldBeEmpty();
}
