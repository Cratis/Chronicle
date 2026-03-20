// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionDefinitionComparer.when_comparing;

public class definitions_with_from_dictionary_in_different_insertion_order : given.a_projection_definition_comparer
{
    static readonly EventType _firstEvent = new("first-event", 1);
    static readonly EventType _secondEvent = new("second-event", 1);

    ProjectionDefinition _first;
    ProjectionDefinition _second;
    ProjectionDefinitionCompareResult _result;

    void Establish()
    {
        _projectionsStorage.Has(_projectionKey.ProjectionId).Returns(true);

        var fromDefinitionA = new FromDefinition(
            new Dictionary<PropertyPath, string> { ["Name"] = "$event.Name" },
            "$eventSourceId",
            null);

        var fromDefinitionB = new FromDefinition(
            new Dictionary<PropertyPath, string> { ["Value"] = "$event.Value" },
            "$eventSourceId",
            null);

        // First has A then B; second has B then A
        _first = CreateDefinition(from: new Dictionary<EventType, FromDefinition>
        {
            [_firstEvent] = fromDefinitionA,
            [_secondEvent] = fromDefinitionB
        });

        _second = CreateDefinition(from: new Dictionary<EventType, FromDefinition>
        {
            [_secondEvent] = fromDefinitionB,
            [_firstEvent] = fromDefinitionA
        });
    }

    async Task Because() => _result = await _comparer.Compare(_projectionKey, _first, _second);

    [Fact] void should_be_same() => _result.ShouldEqual(ProjectionDefinitionCompareResult.Same);
}
