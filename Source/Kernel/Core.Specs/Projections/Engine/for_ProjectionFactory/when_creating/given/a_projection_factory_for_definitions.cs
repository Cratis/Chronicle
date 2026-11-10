// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating.given;

public class a_projection_factory_for_definitions : a_projection_factory_and_an_every_event_mapping
{
    protected static readonly EventType _slotAssigned = new("SlotAssigned", EventTypeGeneration.First);

    protected const string SlotsReadModelSchema = """
        {
          "type": "object",
          "properties": {
            "id": { "type": "string" },
            "slots": { "type": "object", "additionalProperties": { "type": "string" } },
            "entries": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "entryId": { "type": "string" },
                  "rank": { "type": "integer" },
                  "details": { "type": "object", "properties": { "note": { "type": "string" } } },
                  "tags": { "type": "array", "items": { "type": "string" } }
                }
              }
            }
          }
        }
        """;

    protected const string SlotAssignedSchema = """
        {
          "type": "object",
          "properties": {
            "slot": { "type": "string" },
            "seriesId": { "type": "string" }
          }
        }
        """;

    protected async Task<IProjection> CreateFrom(string propertyPath, string expression) => await _factory.Create(
        _eventStore,
        _namespace,
        CreateProjectionDefinition(
            new Dictionary<EventType, FromDefinition>
            {
                [_slotAssigned] = new(new Dictionary<PropertyPath, string> { [new(propertyPath)] = expression }, PropertyExpression.NotSet, null)
            },
            new Dictionary<PropertyPath, ChildrenDefinition>()),
        CreateReadModelDefinition(),
        [new EventTypeSchema(_slotAssigned, EventTypeOwner.Client, EventTypeSource.Code, await JsonSchema.FromJsonAsync(SlotAssignedSchema))]);

    protected Task<IProjection> CreateWithOrderedChildren(ChildrenOrdering ordering) => _factory.Create(
        _eventStore,
        _namespace,
        CreateProjectionDefinition(
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>
            {
                [new("entries")] = new(
                    new PropertyPath("entryId"),
                    new Dictionary<EventType, FromDefinition>(),
                    new Dictionary<EventType, JoinDefinition>(),
                    new Dictionary<PropertyPath, ChildrenDefinition>(),
                    new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
                    new Dictionary<EventType, RemovedWithDefinition>(),
                    new Dictionary<EventType, RemovedWithJoinDefinition>(),
                    Ordering: ordering)
            }),
        CreateReadModelDefinition(),
        []);

    static ProjectionDefinition CreateProjectionDefinition(IDictionary<EventType, FromDefinition> from, IDictionary<PropertyPath, ChildrenDefinition> children) => new(
        ProjectionOwner.Client,
        EventSequenceId.Log,
        "Core.Slots",
        "Core.Slots",
        true,
        true,
        new(),
        from,
        new Dictionary<EventType, JoinDefinition>(),
        children,
        [],
        new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>(),
        AutoMap: AutoMap.Disabled);

    static ReadModelDefinition CreateReadModelDefinition() =>
        new(
            "Core.Slots",
            "slots",
            "Slots",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [ReadModelGeneration.First] = JsonSchema.FromJson(SlotsReadModelSchema)
            },
            []);
}
