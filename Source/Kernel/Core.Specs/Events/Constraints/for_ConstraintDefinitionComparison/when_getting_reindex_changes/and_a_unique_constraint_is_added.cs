// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintDefinitionComparison.when_getting_reindex_changes;

public class and_a_unique_constraint_is_added : Specification
{
    IConstraintDefinition _added;
    IReadOnlyCollection<ConstraintDefinitionChange> _reindexChanges;

    void Establish() => _added = new UniqueConstraintDefinition(
        "new-unique",
        [new UniqueConstraintEventDefinition("some-event", ["Some"])]);

    void Because() => _reindexChanges = ConstraintDefinitionComparison.GetReindexChanges([], [_added]);

    [Fact] void should_derive_a_single_reindex_change() => _reindexChanges.Count.ShouldEqual(1);
    [Fact] void should_require_reindex_for_the_added_constraint() => _reindexChanges.First().RequiresReindex.ShouldBeTrue();
    [Fact] void should_name_the_added_constraint() => _reindexChanges.First().Name.ShouldEqual((ConstraintName)"new-unique");
}
