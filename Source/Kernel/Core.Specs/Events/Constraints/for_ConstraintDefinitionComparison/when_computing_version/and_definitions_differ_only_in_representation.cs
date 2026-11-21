// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintDefinitionComparison.when_computing_version;

public class and_definitions_differ_only_in_representation : Specification
{
    ConstraintsVersion _canonical;
    ConstraintsVersion _reordered;
    ConstraintsVersion _propertiesReordered;
    ConstraintsVersion _ignoringCasing;

    void Because()
    {
        _canonical = ConstraintDefinitionComparison.ComputeVersion([Unique(["first", "second"], ["A", "B"], null)]);
        _reordered = ConstraintDefinitionComparison.ComputeVersion([Unique(["second", "first", "first"], ["A", "B"], new ConstraintScope())]);
        _propertiesReordered = ConstraintDefinitionComparison.ComputeVersion([Unique(["first", "second"], ["B", "A"], null)]);
        _ignoringCasing = ConstraintDefinitionComparison.ComputeVersion([Unique(["first", "second"], ["A", "B"], null) with { IgnoreCasing = true }]);
    }

    static UniqueConstraintDefinition Unique(string[] eventTypes, string[] properties, ConstraintScope? scope) =>
        new("unique-thing", [.. eventTypes.Select(_ => new UniqueConstraintEventDefinition(_, properties))], [(EventTypeId)"removed-one", (EventTypeId)"removed-two"], false, scope);

    [Fact] void should_produce_the_same_version_for_the_same_meaning() => _reordered.ShouldEqual(_canonical);
    [Fact] void should_produce_another_version_when_the_property_order_changes() => _propertiesReordered.ShouldNotEqual(_canonical);
    [Fact] void should_produce_another_version_when_casing_handling_changes() => _ignoringCasing.ShouldNotEqual(_canonical);
}
