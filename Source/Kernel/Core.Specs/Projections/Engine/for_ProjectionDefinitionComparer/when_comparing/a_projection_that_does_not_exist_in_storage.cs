// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionDefinitionComparer.when_comparing;

public class a_projection_that_does_not_exist_in_storage : given.a_projection_definition_comparer
{
    ProjectionDefinition _first;
    ProjectionDefinition _second;
    ProjectionDefinitionCompareResult _result;

    void Establish()
    {
        _projectionsStorage.Has(_projectionKey.ProjectionId).Returns(false);

        _first = CreateDefinition();
        _second = CreateDefinition();
    }

    async Task Because() => _result = await _comparer.Compare(_projectionKey, _first, _second);

    [Fact] void should_be_new() => _result.ShouldEqual(ProjectionDefinitionCompareResult.New);
}
