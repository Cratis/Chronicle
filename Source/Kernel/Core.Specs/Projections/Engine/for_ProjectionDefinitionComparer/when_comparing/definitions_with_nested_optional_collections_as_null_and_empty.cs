// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionDefinitionComparer.when_comparing;

public class definitions_with_nested_optional_collections_as_null_and_empty : given.a_projection_definition_comparer
{
    ProjectionDefinitionCompareResult _forward;
    ProjectionDefinitionCompareResult _reverse;
    ChildrenDefinition _original;

    async Task Because()
    {
        _projectionsStorage.Has(_projectionKey.ProjectionId).Returns(true);
        _original = new("id", new Dictionary<Concepts.Events.EventType, FromDefinition>(), new Dictionary<Concepts.Events.EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), new(new Dictionary<PropertyPath, string>(), false), new Dictionary<Concepts.Events.EventType, RemovedWithDefinition>(), new Dictionary<Concepts.Events.EventType, RemovedWithJoinDefinition>());
        var empty = _original with { Nested = new Dictionary<PropertyPath, ChildrenDefinition>(), NoAutoMapProperties = [] };
        var first = CreateDefinition(children: new Dictionary<PropertyPath, ChildrenDefinition> { ["items"] = _original }) with
        {
            Nested = new Dictionary<PropertyPath, ChildrenDefinition> { ["details"] = _original }
        };
        var second = CreateDefinition(children: new Dictionary<PropertyPath, ChildrenDefinition> { ["items"] = empty }) with
        {
            Nested = new Dictionary<PropertyPath, ChildrenDefinition> { ["details"] = empty }
        };
        _forward = await _comparer.Compare(_projectionKey, first, second);
        _reverse = await _comparer.Compare(_projectionKey, second, first);
    }

    [Fact] void should_compare_nested_collections_as_unchanged() => _forward.ShouldEqual(ProjectionDefinitionCompareResult.Same);
    [Fact] void should_be_symmetric() => _reverse.ShouldEqual(ProjectionDefinitionCompareResult.Same);
    [Fact] void should_not_mutate_original_nested_collections() => _original.Nested.ShouldBeNull();
    [Fact] void should_not_mutate_original_exclusions() => _original.NoAutoMapProperties.ShouldBeNull();
}
