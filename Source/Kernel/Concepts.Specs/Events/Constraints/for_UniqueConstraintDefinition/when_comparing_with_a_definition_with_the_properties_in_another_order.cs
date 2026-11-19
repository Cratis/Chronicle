// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_UniqueConstraintDefinition;

/// <summary>
/// The properties of an event definition are joined in declaration order to form the claimed value, so another order
/// claims other values. It is a real change and must still be reindexed.
/// </summary>
public class when_comparing_with_a_definition_with_the_properties_in_another_order : Specification
{
    static readonly ConstraintName _name = "some-constraint";

    UniqueConstraintDefinition _definition;
    UniqueConstraintDefinition _other;

    void Establish()
    {
        _definition = new(_name, [new("the-event", ["A", "B"])]);
        _other = new(_name, [new("the-event", ["B", "A"])]);
    }

    [Fact] void should_not_be_equal() => _definition.Equals(_other).ShouldBeFalse();
    [Fact] void should_require_a_reindex() => _definition.CompareWith(_other).RequiresReindex.ShouldBeTrue();
    [Fact] void should_report_the_indexed_properties_as_changed() => _definition.CompareWith(_other).ChangeTypes.ShouldContainOnly(ConstraintChangeType.IndexedPropertiesChanged);
}
