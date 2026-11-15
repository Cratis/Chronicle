// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionDefinitionInitialization.given;

public class a_definition_with_children : for_ProjectionFactory.when_creating.given.a_projection_factory_and_an_every_event_mapping
{
    protected static readonly EventType Event = new("Created", 1);
    protected static readonly FromDefinition From = new(new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet, null);
    protected static readonly JoinDefinition Join = new("id", new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet);
    protected ProjectionDefinition _definition;
    protected ReadModelDefinition _readModel;

    void Establish()
    {
        _definition = new(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "projection",
            "model",
            true,
            true,
            new(),
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition> { ["entries"] = Child() },
            [],
            new(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            AutoMap: AutoMap.Disabled);
        _readModel = new(
            "model",
            "models",
            "Models",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            "projection",
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [ReadModelGeneration.First] = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"string"},"details":{"type":"object"},"entries":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"}}}}}}""")
            },
            []);
    }

    protected static ChildrenDefinition Child() => new("id", new Dictionary<EventType, FromDefinition>(), new Dictionary<EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), new(new Dictionary<PropertyPath, string>(), false), new Dictionary<EventType, RemovedWithDefinition>(), new Dictionary<EventType, RemovedWithJoinDefinition>(), AutoMap: AutoMap.Disabled);

    protected async Task<bool> MatchesCompiledInitialization(ProjectionDefinition definition)
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"}}}""");
        var compiled = await _factory.Create(_eventStore, _namespace, definition, _readModel, [new EventTypeSchema(Event, EventTypeOwner.Client, EventTypeSource.Code, schema)]);
        return ProjectionDefinitionInitialization.IsChildOnly(definition) == ProjectionInitialization.IsChildOnly(compiled);
    }
}
