// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Storage.Sinks;
using Neo4j.Driver;
using NSubstitute.Extensions;
using static Cratis.Chronicle.Storage.Neo4j.Neo4jSinkProvenanceStatements;

namespace Cratis.Chronicle.Storage.Neo4j.for_Neo4jSinkProvenanceStorage.given;

public class a_graph_database : Specification
{
    protected static readonly Uri Endpoint = new("bolt://server:7687");
    protected readonly IAsyncQueryRunner _runtime = Substitute.For<IAsyncQueryRunner>();
    protected readonly IAsyncQueryRunner _schema = Substitute.For<IAsyncQueryRunner>();
    protected ISinkProvenanceStorage _provenance;

    protected virtual string SchemaDatabaseId => "physical";

    void Establish() => _provenance = StorageFor(_runtime, _schema, "physical", SchemaDatabaseId);

    protected static ISinkProvenanceStorage StorageFor(
        IAsyncQueryRunner runtimeRunner,
        IAsyncQueryRunner schemaRunner,
        string physicalId) => StorageFor(runtimeRunner, schemaRunner, physicalId, physicalId);

    protected static IRecord Protection()
    {
        var record = Substitute.For<IRecord>();
        record["state"].Returns(new Dictionary<string, object> { ["_key"] = "protected", ["_format"] = 1L });
        return record;
    }

    protected static IRecord Inventory(Guid id, long version = 0L, bool restricted = false)
    {
        var record = Substitute.For<IRecord>();
        record["state"].Returns(new Dictionary<string, object>
        {
            ["_key"] = "inventory",
            ["_format"] = 1L,
            ["_id"] = id.ToString("D"),
            ["_version"] = version,
            ["_restricted"] = restricted
        });
        return record;
    }

    protected static IResultCursor Cursor(params IRecord[] records)
    {
        var cursor = Substitute.For<IResultCursor>();
        var index = -1;
        cursor.FetchAsync().Returns(_ => ++index < records.Length);
        cursor.Current.Returns(_ => records[index]);
        return cursor;
    }

    static Neo4jSinkProvenanceStorage StorageFor(
        IAsyncQueryRunner runtimeRunner,
        IAsyncQueryRunner schemaRunner,
        string physicalId,
        string schemaId)
    {
        var connections = Substitute.For<INeo4jConnections>();
        var runtime = new Neo4jConnection(DriverFor(runtimeRunner), "neo4j", false) { Endpoint = Endpoint };
        var schema = new Neo4jConnection(DriverFor(schemaRunner), "neo4j", false) { Endpoint = Endpoint };
        connections.Resolve("store", "scope", ConnectionPurpose.Runtime).Returns(runtime);
        connections.Resolve("store", "scope", ConnectionPurpose.Schema).Returns(schema);
        SetIdentity(runtimeRunner, physicalId);
        SetIdentity(schemaRunner, schemaId);
        schemaRunner.Configure().RunAsync(CreateConstraint, Arg.Any<object>()).Returns(_ => Cursor());
        schemaRunner.Configure().RunAsync(VerifyConstraint).Returns(_ => Cursor(Substitute.For<IRecord>()));
        return new(new Neo4jGraphTarget(connections, "store", "scope"));
    }

    static void SetIdentity(IAsyncQueryRunner runner, string id)
    {
        var record = Substitute.For<IRecord>();
        record["id"].Returns(id);
        runner.Configure().RunAsync("CALL db.info() YIELD id RETURN id").Returns(_ => Cursor(record));
    }

    static IDriver DriverFor(IAsyncQueryRunner runner)
    {
        var session = Substitute.For<IAsyncSession>();
        session.ExecuteWriteAsync(Arg.Any<Func<IAsyncQueryRunner, Task>>(), Arg.Any<Action<TransactionConfigBuilder>>())
            .Returns(_ => _.Arg<Func<IAsyncQueryRunner, Task>>()(runner));
        session.ExecuteWriteAsync(Arg.Any<Func<IAsyncQueryRunner, Task<SinkProvenanceState>>>(), Arg.Any<Action<TransactionConfigBuilder>>())
            .Returns(_ => _.Arg<Func<IAsyncQueryRunner, Task<SinkProvenanceState>>>()(runner));
        var driver = Substitute.For<IDriver>();
        driver.AsyncSession(Arg.Any<Action<SessionConfigBuilder>>()).Returns(session);
        return driver;
    }
}
