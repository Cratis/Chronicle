// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Graphs;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Graphs;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Graphs;
using Cratis.Chronicle.Storage.InMemory.Sinks.Graphs;
using Microsoft.Extensions.Logging.Abstractions;
using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.Storage.InMemory.Projections.Graphs.for_GraphProjectionPipeline.given;

/// <summary>
/// The Organization graph projected through the real plan factory and pipeline onto the in-memory graph sink.
/// </summary>
public class a_graph_projection : Specification
{
    protected static readonly EventType PersonRegistered = new("PersonRegistered", EventTypeGeneration.First);
    protected static readonly EventType PersonVisited = new("PersonVisited", EventTypeGeneration.First);
    protected static readonly EventType PersonRemoved = new("PersonRemoved", EventTypeGeneration.First);
    protected static readonly EventType GroupCreated = new("GroupCreated", EventTypeGeneration.First);
    protected static readonly EventType MemberAdded = new("MemberAdded", EventTypeGeneration.First);
    protected static readonly EventType MemberRemoved = new("MemberRemoved", EventTypeGeneration.First);

    protected InMemoryGraphSink _sink;
    protected GraphProjectionPipeline _pipeline;
    protected ProjectionId _projectionId = "organization";

    protected static GraphDefinition Graph => new(
        "Organization",
        [
            new("Person", "personId", Schema(("personId", JsonObjectType.String), ("name", JsonObjectType.String), ("visits", JsonObjectType.Integer))),
            new("Group", "groupId", Schema(("groupId", JsonObjectType.String), ("name", JsonObjectType.String), ("members", JsonObjectType.Integer)))
        ],
        [
            new("MEMBER_OF", "Person", "Group", null, Schema(("since", JsonObjectType.String)))
        ]);

    protected static GraphProjectionDefinition Definition => new(
        new Dictionary<string, GraphNodeProjectionDefinition>
        {
            ["Person"] = new(
                new Dictionary<string, GraphNodeFromDefinition>
                {
                    [PersonRegistered.ToString()] = new("personId", new Dictionary<string, string> { ["name"] = "name" }),
                    [PersonVisited.ToString()] = new("personId", new Dictionary<string, string> { ["visits"] = "$increment" })
                },
                new Dictionary<string, GraphNodeRemovedWithDefinition> { [PersonRemoved.ToString()] = new("personId", true) }),
            ["Group"] = new(
                new Dictionary<string, GraphNodeFromDefinition>
                {
                    [GroupCreated.ToString()] = new("groupId", new Dictionary<string, string> { ["name"] = "name" }),
                    [MemberAdded.ToString()] = new("groupId", new Dictionary<string, string> { ["members"] = "$increment" })
                },
                new Dictionary<string, GraphNodeRemovedWithDefinition>())
        },
        new Dictionary<string, GraphEdgeProjectionDefinition>
        {
            ["MEMBER_OF"] = new(
                new Dictionary<string, GraphEdgeFromDefinition>
                {
                    [MemberAdded.ToString()] = new("personId", "groupId", null, new Dictionary<string, string> { ["since"] = "since" })
                },
                new Dictionary<string, GraphEdgeRemovedWithDefinition> { [MemberRemoved.ToString()] = new("personId", "groupId", null) })
        });

    protected static AppendedEvent Event(EventType type, ulong sequence, string eventSourceId, params (string Name, object Value)[] content)
    {
        var expando = new ExpandoObject();
        foreach (var (name, value) in content) ((IDictionary<string, object?>)expando)[name] = value;
        return new(EventContext.Empty with { EventType = type, SequenceNumber = sequence, EventSourceId = eventSourceId }, expando);
    }

    protected Task<GraphNode?> Person(string key) => _sink.FindNode(_projectionId, new NodeReference("Person", key));

    protected Task<GraphNode?> Group(string key) => _sink.FindNode(_projectionId, new NodeReference("Group", key));

    protected Task<GraphEdge?> MemberOf(string person, string group) =>
        _sink.FindEdge(_projectionId, new EdgeReference("MEMBER_OF", new("Person", person), new("Group", group)));

    protected GraphProjectionPlan CreatePlan(GraphProjectionDefinition definition, GraphDefinition graph)
    {
        var typeFormats = new TypeFormats();
        var eventValues = new EventValueProviderExpressionResolvers(typeFormats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        var properties = new ReadModelPropertyExpressionResolvers(eventValues, typeFormats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance);
        return new GraphProjectionPlanFactory(properties, eventValues).Create(_projectionId, definition, graph);
    }

    void Establish()
    {
        _sink = new InMemoryGraphSink(Graph);
        var projection = Substitute.For<EngineProjection>();
        projection.Identifier.Returns(_projectionId);
        _pipeline = new GraphProjectionPipeline(
            projection,
            new GraphChangesetBuilder(CreatePlan(Definition, Graph), _sink, _projectionId),
            _sink,
            new ProjectionHandleLock(),
            new ObjectComparer(),
            NullLogger<GraphProjectionPipeline>.Instance);
    }

    static JsonSchema Schema(params (string Name, JsonObjectType Type)[] properties)
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        foreach (var (name, type) in properties) schema.Properties[name] = new JsonSchemaProperty { Type = type };
        return schema;
    }
}
