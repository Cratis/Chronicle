// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintDefinitionComparison.when_getting_reindex_changes;

public class and_the_definition_only_lists_the_same_things_in_another_order : Specification
{
    IReadOnlyCollection<ConstraintDefinitionChange> _reindexChanges;
    IReadOnlyCollection<ConstraintDefinitionChange> _propertyChanges;

    void Because()
    {
        var previous = new UniqueConstraintDefinition("some-unique", [new("first", ["A"]), new("second", ["B"])], ["removed-one", "removed-two"]);
        var reordered = new UniqueConstraintDefinition("some-unique", [new("second", ["B"]), new("first", ["A"])], ["removed-two", "removed-one"]);
        var propertiesChanged = new UniqueConstraintDefinition("some-unique", [new("second", ["B", "C"]), new("first", ["A"])], ["removed-two", "removed-one"]);
        _reindexChanges = ConstraintDefinitionComparison.GetReindexChanges([previous], [reordered], EventSequenceId.Log);
        _propertyChanges = ConstraintDefinitionComparison.GetReindexChanges([previous], [propertiesChanged], EventSequenceId.Log);
    }

    [Fact] void should_not_require_a_reindex() => _reindexChanges.ShouldBeEmpty();
    [Fact] void should_require_a_reindex_when_the_properties_really_changed() => _propertyChanges.Count.ShouldEqual(1);
}
