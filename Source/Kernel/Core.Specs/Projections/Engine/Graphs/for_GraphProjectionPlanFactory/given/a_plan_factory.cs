// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Graphs;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Graphs.for_GraphProjectionPlanFactory.given;

public class a_plan_factory : Specification
{
    protected GraphProjectionPlanFactory _factory;

    protected static GraphDefinition Graph => new(
        "Organization",
        [
            new("Person", "personId", Schema(("personId", JsonObjectType.String), ("name", JsonObjectType.String), ("address", JsonObjectType.Object))),
            new("Group", "groupId", Schema(("groupId", JsonObjectType.String)))
        ],
        [
            new("MEMBER_OF", "Person", "Group", null, Schema(("since", JsonObjectType.String))),
            new("VISITED", "Person", "Group", "visitId", Schema(("visitId", JsonObjectType.String)))
        ]);

    protected static GraphProjectionDefinition Nodes(string label, GraphNodeProjectionDefinition node) =>
        new(new Dictionary<string, GraphNodeProjectionDefinition> { [label] = node }, new Dictionary<string, GraphEdgeProjectionDefinition>());

    protected static GraphProjectionDefinition Edges(string type, GraphEdgeProjectionDefinition edge) =>
        new(new Dictionary<string, GraphNodeProjectionDefinition>(), new Dictionary<string, GraphEdgeProjectionDefinition> { [type] = edge });

    protected static GraphNodeProjectionDefinition NodeFrom(string eventType, params (string Property, string Expression)[] properties) =>
        new(
            new Dictionary<string, GraphNodeFromDefinition> { [eventType] = new("personId", properties.ToDictionary(_ => _.Property, _ => _.Expression)) },
            new Dictionary<string, GraphNodeRemovedWithDefinition>());

    protected static GraphEdgeProjectionDefinition EdgeFrom(string eventType, string? identity = null) =>
        new(
            new Dictionary<string, GraphEdgeFromDefinition> { [eventType] = new("personId", "groupId", identity is null ? null : new PropertyExpression(identity), new Dictionary<string, string>()) },
            new Dictionary<string, GraphEdgeRemovedWithDefinition>());

    void Establish()
    {
        var typeFormats = new TypeFormats();
        var eventValues = new EventValueProviderExpressionResolvers(typeFormats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        var properties = new ReadModelPropertyExpressionResolvers(eventValues, typeFormats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance);
        _factory = new GraphProjectionPlanFactory(properties, eventValues);
    }

    static JsonSchema Schema(params (string Name, JsonObjectType Type)[] properties)
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        foreach (var (name, type) in properties) schema.Properties[name] = new JsonSchemaProperty { Type = type };
        return schema;
    }
}
