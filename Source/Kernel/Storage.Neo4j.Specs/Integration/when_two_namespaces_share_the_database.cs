// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Storage.Graphs;
using Neo4j.Driver;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;

namespace Cratis.Chronicle.Storage.Neo4j.Integration;

/// <summary>
/// Single database mode against a live server: the namespaces keep their own graphs in one database, every element carries
/// the bookkeeping, and the schema is created once.
/// </summary>
public class when_two_namespaces_share_the_database : Specification
{
    readonly Neo4jDriverFactory _driverFactory = new();
    string _first;
    string _second;
    string _nodeInFirstAfterDeletingInSecond;
    string _nodeInSecond;
    string _nodeInFirstAfterSweepingSecond;
    IReadOnlyDictionary<string, object> _stored;
    long _constraints;
    long _nodesInSecond;

    async Task Because()
    {
        if (!LiveNeo4j.Enabled) return;

        var connections = LiveNeo4j.CreateConnections(_driverFactory);
        _first = $"{LiveNeo4j.NamespacePrefix}{Guid.NewGuid():N}";
        _second = $"{LiveNeo4j.NamespacePrefix}{Guid.NewGuid():N}";
        var inFirst = new Neo4jGraphSink(GraphDefinitions.Organization, "specs", _first, connections);
        var inSecond = new Neo4jGraphSink(GraphDefinitions.Organization, "specs", _second, connections);
        await inFirst.EnsureSchema();
        await inFirst.EnsureSchema();
        await inSecond.EnsureSchema();

        var first = new GraphScenarioRun(inFirst);
        var second = new GraphScenarioRun(inSecond);
        await first.Apply(7, new UpsertNode(Person("p1"), Props(("personId", "p1"), ("name", "First"))));
        await second.Apply(8, new UpsertNode(Person("p1"), Props(("personId", "p1"), ("name", "Second"))), new UpsertEdge(MemberOf("p1", "g1"), Props()));
        await second.Apply(9, new DeleteNode(Person("p1"), true));
        _nodeInFirstAfterDeletingInSecond = GraphDump.Describe(await inFirst.FindNode(Owner, Person("p1")));

        await second.Apply(10, new UpsertNode(Person("p2"), Props(("personId", "p2"), ("name", "Stale"))), new UpsertNode(Person("p3"), Props(("personId", "p3"), ("name", "Stale"))));
        var context = ReplayStartedAt(10);
        await inSecond.BeginReplay(Owner, context);
        await second.Apply(2, new UpsertNode(Person("p2"), Props(("personId", "p2"), ("name", "Other"))));
        await inSecond.EndReplay(Owner, context);
        _nodeInSecond = GraphDump.Describe(await inSecond.FindNode(Owner, Person("p2")));
        _nodeInFirstAfterSweepingSecond = GraphDump.Describe(await inFirst.FindNode(Owner, Person("p1")));
        _nodesInSecond = (await inSecond.GetNodes("Person")).TotalCount;

        var connection = await connections.Resolve("specs", _first, ConnectionPurpose.Runtime);
        await using var session = connection.OpenSession();
        var cursor = await session.RunAsync("MATCH (n:Person {personId: 'p1', `_namespace`: $namespace}) RETURN properties(n) AS properties", new Dictionary<string, object> { ["namespace"] = _first });
        _stored = (await cursor.ToListAsync())[0]["properties"].As<IReadOnlyDictionary<string, object>>();
        var constraints = await (await session.RunAsync("SHOW CONSTRAINTS YIELD name WHERE name STARTS WITH 'chronicle_' RETURN count(name) AS total")).ToListAsync();
        _constraints = constraints[0]["total"].As<long>();
    }

    async Task Destroy()
    {
        if (LiveNeo4j.Enabled) await LiveNeo4j.Clean(_driverFactory, [_first, _second]);
        await _driverFactory.DisposeAsync();
    }

    [LiveNeo4jFact] void should_not_let_a_delete_in_one_namespace_reach_the_other() => _nodeInFirstAfterDeletingInSecond.ShouldContain("name='First'");
    [LiveNeo4jFact] void should_not_let_a_sweep_in_one_namespace_reach_the_other() => _nodeInFirstAfterSweepingSecond.ShouldContain("name='First'");
    [LiveNeo4jFact] void should_keep_the_node_written_in_the_replay_of_the_namespace() => _nodeInSecond.ShouldContain("name='Other'");
    [LiveNeo4jFact] void should_sweep_the_stale_nodes_of_the_namespace() => _nodesInSecond.ShouldEqual(1L);
    [LiveNeo4jFact] void should_stamp_the_namespace() => _stored["_namespace"].ShouldEqual(_first);
    [LiveNeo4jFact] void should_stamp_the_watermark() => _stored["_watermark"].ShouldEqual(7L);
    [LiveNeo4jFact] void should_stamp_the_owner() => _stored["_owner"].ShouldEqual("owner");
    [LiveNeo4jFact] void should_stamp_the_generation() => _stored.ContainsKey("_generation").ShouldBeTrue();
    [LiveNeo4jFact] void should_store_the_key_under_the_name_of_the_definition() => _stored["personId"].ShouldEqual("p1");
    [LiveNeo4jFact] void should_create_the_constraints_and_the_index_once() => (_constraints >= 3).ShouldBeTrue();
}
