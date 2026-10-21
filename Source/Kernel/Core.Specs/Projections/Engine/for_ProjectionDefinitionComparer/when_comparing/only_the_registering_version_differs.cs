// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionDefinitionComparer.when_comparing;

public class only_the_registering_version_differs : given.a_projection_definition_comparer
{
    ProjectionDefinitionCompareResult _result;

    void Establish() => _projectionsStorage.Has(_projectionKey.ProjectionId).Returns(true);

    async Task Because() => _result = await _comparer.Compare(
        _projectionKey,
        CreateDefinition() with { Version = "1.0.0" },
        CreateDefinition() with { Version = "2.0.0" });

    [Fact] void should_not_trigger_a_replay() => _result.ShouldEqual(ProjectionDefinitionCompareResult.Same);
}
