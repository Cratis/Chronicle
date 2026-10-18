// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionDefinitionComparer.when_comparing;

public class definitions_with_optional_collections_as_null_and_empty : given.a_projection_definition_comparer
{
    ProjectionDefinition _first;
    ProjectionDefinition _second;
    ProjectionDefinitionCompareResult _result;
    ProjectionDefinitionCompareResult _reverse;

    void Establish()
    {
        _projectionsStorage.Has(_projectionKey.ProjectionId).Returns(true);

        _first = CreateDefinition() with
        {
            Tags = null,
            Nested = null,
            NoAutoMapProperties = null
        };

        _second = CreateDefinition() with
        {
            Tags = [],
            Nested = new Dictionary<PropertyPath, ChildrenDefinition>(),
            NoAutoMapProperties = []
        };
    }

    async Task Because()
    {
        _result = await _comparer.Compare(_projectionKey, _first, _second);
        _reverse = await _comparer.Compare(_projectionKey, _second, _first);
    }

    [Fact] void should_be_same() => _result.ShouldEqual(ProjectionDefinitionCompareResult.Same);
    [Fact] void should_be_symmetric() => _reverse.ShouldEqual(ProjectionDefinitionCompareResult.Same);
}
