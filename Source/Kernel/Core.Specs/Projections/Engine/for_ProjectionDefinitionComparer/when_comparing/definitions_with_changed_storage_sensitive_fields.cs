// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionDefinitionComparer.when_comparing;

public class definitions_with_changed_storage_sensitive_fields : given.a_projection_definition_comparer
{
    ProjectionDefinitionCompareResult _scope;
    ProjectionDefinitionCompareResult _key;
    ProjectionDefinitionCompareResult _exclusions;

    async Task Because()
    {
        _projectionsStorage.Has(_projectionKey.ProjectionId).Returns(true);
        var definition = CreateDefinition();
        _scope = await _comparer.Compare(_projectionKey, definition, definition with { Scope = ProjectionScope.Global });
        _key = await _comparer.Compare(_projectionKey, definition, definition with { FromEvery = definition.FromEvery with { Key = "Category" } });
        _exclusions = await _comparer.Compare(_projectionKey, definition, definition with { NoAutoMapProperties = ["name"] });
    }

    [Fact] void should_detect_a_changed_scope() => _scope.ShouldEqual(ProjectionDefinitionCompareResult.Different);
    [Fact] void should_detect_a_changed_key() => _key.ShouldEqual(ProjectionDefinitionCompareResult.Different);
    [Fact] void should_detect_changed_auto_map_exclusions() => _exclusions.ShouldEqual(ProjectionDefinitionCompareResult.Different);
}
