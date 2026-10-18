// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory;

public class when_fingerprinting_definitions : when_creating.given.a_child_collection_projection
{
    readonly EventType _eventType = new("event", 1);

    void Establish() => _definition.From[_eventType] = new(new Dictionary<PropertyPath, string> { ["name"] = "name", ["value"] = "value" }, "$eventSourceId", null);

    [Theory]
    [InlineData("metadata")]
    [InlineData("dictionary-order")]
    void should_ignore_nonstructural_differences(string change)
    {
        var other = change == "metadata"
            ? _definition with { Version = "2.0.0", LastUpdated = DateTimeOffset.UtcNow, Tags = ["tag"] }
            : _definition with { From = new Dictionary<EventType, FromDefinition> { [_eventType] = _definition.From[_eventType] with { Properties = new Dictionary<PropertyPath, string> { ["value"] = "value", ["name"] = "name" } } } };
        ProjectionFactory.DefinitionFingerprintFor(other).ShouldEqual(ProjectionFactory.DefinitionFingerprintFor(_definition));
    }

    [Theory]
    [InlineData("mapping")]
    [InlineData("child-mapping")]
    [InlineData("read-model")]
    [InlineData("initial-state")]
    [InlineData("pin")]
    void should_detect_pipeline_affecting_differences(string change)
    {
        var other = change switch
        {
            "mapping" => _definition with { From = new Dictionary<EventType, FromDefinition> { [_eventType] = _definition.From[_eventType] with { Properties = new Dictionary<PropertyPath, string> { ["name"] = "different", ["value"] = "value" } } } },
            "child-mapping" => _definition with { Children = new Dictionary<PropertyPath, ChildrenDefinition> { ["notes"] = Child() with { From = _definition.From } } },
            "read-model" => _definition with { ReadModel = "another" },
            "initial-state" => _definition with { InitialModelState = new JsonObject { ["count"] = 2 } },
            _ => _definition with { GenerationDelivery = EventGenerationDelivery.Pinned, From = new Dictionary<EventType, FromDefinition> { [new(_eventType.Id, 2)] = _definition.From[_eventType] } }
        };
        ProjectionFactory.DefinitionFingerprintFor(other).ShouldNotEqual(ProjectionFactory.DefinitionFingerprintFor(_definition));
    }
}
