// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Connections;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Graphs;
using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j.for_Neo4jGraphSink;

/// <summary>
/// Asserts which database the sink opens its sessions on, since a database per event store and namespace cannot run on a
/// server without multiple databases.
/// </summary>
public class when_applying_a_changeset : Specification
{
    IDriver _driver;
    IAsyncSession _session;
    INeo4jConnections _connections;
    Neo4jGraphSink _sink;
    List<Action<SessionConfigBuilder>> _configurations;
    string _database;

    void Establish()
    {
        _configurations = [];
        _session = Substitute.For<IAsyncSession>();
        _driver = Substitute.For<IDriver>();
        _driver.AsyncSession(Arg.Do<Action<SessionConfigBuilder>>(_configurations.Add)).Returns(_session);
        _connections = Substitute.For<INeo4jConnections>();
        _connections.Options.Returns(new Neo4jOptions());
        _connections.Resolve(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), ConnectionPurpose.Runtime)
            .Returns(new Neo4jConnection(_driver, GraphDatabaseName.For(null, "Orders", "acme"), true));
        _sink = new(GraphDefinitions.Organization, "Orders", "acme", _connections);
    }

    async Task Because()
    {
        await _sink.ApplyGraphChanges(new GraphChangeset("owner", [new DeleteEdge(new EdgeReference("MEMBER_OF", new NodeReference("Person", "p1"), new NodeReference("Group", "g1")))]), 1);
        _database = DatabaseOf(_configurations[0]);
    }

    static string DatabaseOf(Action<SessionConfigBuilder> configure)
    {
        var config = (SessionConfig)Activator.CreateInstance(typeof(SessionConfig), true)!;
        var builder = (SessionConfigBuilder)Activator.CreateInstance(typeof(SessionConfigBuilder), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, [config], null)!;
        configure(builder);
        return config.Database;
    }

    [Fact] void should_open_one_session() => _configurations.Count.ShouldEqual(1);
    [Fact] void should_open_the_session_on_the_database_of_the_event_store_and_namespace() => _database.ShouldEqual("orders-acme");
    [Fact] void should_run_in_one_write_transaction() => _session.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == "ExecuteWriteAsync").ShouldEqual(1);
}
