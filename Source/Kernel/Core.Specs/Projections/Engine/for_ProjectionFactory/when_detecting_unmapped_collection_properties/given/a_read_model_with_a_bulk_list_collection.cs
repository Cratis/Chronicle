// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_detecting_unmapped_collection_properties.given;

/// <summary>
/// A read model whose child body carries a bulk <c>annotations</c> list — the shape that silently projects
/// empty when the event's list property is named differently and nothing bridges the two.
/// </summary>
public class a_read_model_with_a_bulk_list_collection : Specification
{
    protected static readonly EventType _lineAdded = new("NotedLineAdded", EventTypeGeneration.First);

    protected static readonly JsonSchema _readModelSchema = JsonSchema.FromJson(
        """
        {
          "type": "object",
          "properties": {
            "id": { "type": "string" },
            "description": { "type": "string" },
            "annotations": { "type": "array", "items": { "type": "object", "properties": { "text": { "type": "string" } } } }
          }
        }
        """);

    protected static IEnumerable<EventTypeSchema> EventWithListNamed(string listProperty) =>
    [
        new EventTypeSchema(
            _lineAdded,
            EventTypeOwner.Client,
            EventTypeSource.User,
            JsonSchema.FromJson(
                $$"""
                {
                  "type": "object",
                  "properties": {
                    "description": { "type": "string" },
                    "{{listProperty}}": { "type": "array", "items": { "type": "object" } }
                  }
                }
                """))
    ];

    protected static ProjectionDefinition ProjectionFrom(IDictionary<PropertyPath, string>? properties = default) =>
        new(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "test-projection",
            "NotedLine",
            true,
            true,
            new(),
            new Dictionary<EventType, FromDefinition>
            {
                [_lineAdded] = new(properties ?? new Dictionary<PropertyPath, string>(), WellKnownExpressions.EventSourceId, null)
            },
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>());
}
