// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionDefinitionComparer.when_comparing;

public class definitions_that_are_identical : given.a_projection_definition_comparer
{
    static readonly EventType _eventType = new("some-event", 1);

    ProjectionDefinition _first;
    ProjectionDefinition _second;
    ProjectionDefinitionCompareResult _result;

    void Establish()
    {
        _projectionsStorage.Has(_projectionKey.ProjectionId).Returns(true);

        var from = new Dictionary<EventType, FromDefinition>
        {
            [_eventType] = new(new Dictionary<PropertyPath, string> { ["SomeProp"] = "$event.SomeProp" }, "$eventSourceId", null)
        };

        _first = CreateDefinition(from: from);
        _second = CreateDefinition(from: from);
    }

    async Task Because() => _result = await _comparer.Compare(_projectionKey, _first, _second);

    [Fact] void should_be_same() => _result.ShouldEqual(ProjectionDefinitionCompareResult.Same);
}
