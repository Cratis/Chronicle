// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_LegacyChildEveryEventMappings.given;

public class a_legacy_definition : Specification
{
    protected ProjectionDefinition _definition;
    protected ProjectionDefinition _result;
    protected JsonSchema _schema;

    void Establish()
    {
        _definition = new(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "legacy",
            "legacy-model",
            true,
            true,
            new(),
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition> { ["children"] = Child() },
            [],
            new(new Dictionary<PropertyPath, string> { ["lastValue"] = "value", ["source"] = "$eventContext(eventSourceId)" }, true),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>());
        _schema = JsonSchema.FromJson("""
            {"type":"object","properties":{
              "source":{"type":"string"},
              "children":{"type":"array","items":{"type":"object","properties":{
                "id":{"type":"string"},"lastValue":{"type":"string"}
              }}}
            }}
            """);
    }

    protected static ChildrenDefinition Child() => new(
        "id",
        new Dictionary<EventType, FromDefinition>(),
        new Dictionary<EventType, JoinDefinition>(),
        new Dictionary<PropertyPath, ChildrenDefinition>(),
        new(new Dictionary<PropertyPath, string>(), false),
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>());
}
