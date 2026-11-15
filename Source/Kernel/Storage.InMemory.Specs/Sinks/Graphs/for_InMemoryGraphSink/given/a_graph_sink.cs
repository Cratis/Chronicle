// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Graphs;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs.for_InMemoryGraphSink.given;

/// <summary>
/// A graph with Person and Group nodes, a MEMBER_OF edge and a multi-edge type identified by a property.
/// </summary>
public class a_graph_sink : Specification
{
    protected static readonly ProjectionId Owner = "owner";
    protected static readonly ProjectionId OtherOwner = "other";

    protected InMemoryGraphSink _sink;

    protected static NodeReference Person(string key) => new("Person", key);

    protected static NodeReference Group(string key) => new("Group", key);

    protected static EdgeReference MemberOf(string person, string group) => new("MEMBER_OF", Person(person), Group(group));

    protected static EdgeReference Visited(string person, string group, string identity) => new("VISITED", Person(person), Group(group), identity);

    protected static ReplayContext ReplayStartedAt(int seconds) =>
        new(new("graph", 1), "container", "revert", new DateTimeOffset(2026, 1, 1, 0, 0, seconds, TimeSpan.Zero));

    protected static IReadOnlyDictionary<string, object?> Props(params (string Name, object? Value)[] properties) =>
        properties.ToDictionary(_ => _.Name, _ => _.Value);

    protected Task Apply(ulong sequence, params GraphOperation[] operations) => Apply(Owner, sequence, operations);

    protected Task Apply(ProjectionId owner, ulong sequence, params GraphOperation[] operations) =>
        _sink.ApplyGraphChanges(new GraphChangeset(owner, operations), sequence);

    protected async Task<GraphNode?> NodeOf(NodeReference node) => await _sink.FindNode(Owner, node);

    protected async Task<GraphEdge?> EdgeOf(EdgeReference edge) => await _sink.FindEdge(Owner, edge);

    void Establish()
    {
        var definition = new GraphDefinition(
            "Organization",
            [
                new("Person", "personId", Schema(("personId", "string"), ("name", "string"), ("visits", "integer"))),
                new("Group", "groupId", Schema(("groupId", "string"), ("name", "string")))
            ],
            [
                new("MEMBER_OF", "Person", "Group", null, Schema(("since", "string"))),
                new("VISITED", "Person", "Group", "visitId", Schema(("visitId", "string"), ("count", "integer")))
            ]);
        _sink = new InMemoryGraphSink(definition);
    }

    static JsonSchema Schema(params (string Name, string Type)[] properties)
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        foreach (var (name, type) in properties)
        {
            schema.Properties[name] = new JsonSchemaProperty { Type = type == "integer" ? JsonObjectType.Integer : JsonObjectType.String };
        }

        return schema;
    }
}
