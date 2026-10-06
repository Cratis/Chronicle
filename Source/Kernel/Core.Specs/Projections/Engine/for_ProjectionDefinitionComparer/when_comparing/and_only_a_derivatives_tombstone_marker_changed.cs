// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionDefinitionComparer.when_comparing;

public class and_only_a_derivatives_tombstone_marker_changed : given.a_projection_definition_comparer
{
    ProjectionDefinition _first;
    ProjectionDefinition _second;
    ProjectionDefinitionCompareResult _result;

    void Establish()
    {
        _projectionsStorage.Has(_projectionKey.ProjectionId).Returns(true);
        var type = new EventType("some-event", 1);
        var from = new FromDefinition(new Dictionary<PropertyPath, string>(), "$eventSourceId", null);
        _first = CreateDefinition(fromDerivatives: [new([type], from)]);
        _second = CreateDefinition(fromDerivatives: [new([type with { Tombstone = true }], from)]);
    }

    async Task Because() => _result = await _comparer.Compare(_projectionKey, _first, _second);

    [Fact] void should_be_the_same() => _result.ShouldEqual(ProjectionDefinitionCompareResult.Same);
}
