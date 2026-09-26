// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SqlSink = Cratis.Chronicle.Storage.Sql.Sinks.Sink;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes;

public class and_a_keyed_from_shares_the_join_event : Specification
{
    readonly JsonSchema _schema = JsonSchema.FromJson("""
        {"type":"object","properties":{"id":{"type":"string"},"joinId":{"type":"string"},"count":{"type":"integer"}}}
        """);
    SqliteConnection _connection;
    IReadOnlyList<ProjectedColumn> _columns;
    SqlSink _sink;
    ExpandoObject? _result;
    int _rowCount;

    async Task Establish()
    {
        _columns = ProjectedColumns.ForSchema(_schema);
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();
        await using (var context = CreateContext())
        {
            await context.Database.EnsureCreatedAsync();
        }

        var database = Substitute.For<IDatabase>();
        database.ReadModelTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ProjectedColumn>>())
            .Returns(_ => Task.FromResult(new DbContextScope<ReadModelDbContext>(CreateContext(), () => { })));
        var readModel = new ReadModelDefinition(
            "test",
            "test",
            "test",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [ReadModelGeneration.First] = _schema
            },
            []);
        _sink = new SqlSink("test", "test", readModel, database, new ExpandoObjectConverter(new TypeFormats()));
    }

    async Task Because()
    {
        var initial = new ExpandoObject();
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(initial);
        changeset.Changes.Returns(
        [
            new PropertiesChanged<ExpandoObject>(initial, [new PropertyDifference(new PropertyPath("count"), 0L, 1L)]),
            new Joined(initial, "root", new PropertyPath("joinId"), ArrayIndexers.NoIndexers, [])
            {
                HasKeyedFrom = true
            }
        ]);
        await _sink.ApplyChanges(new Key("root", ArrayIndexers.NoIndexers), changeset, EventSequenceNumber.First);
        _result = await _sink.FindOrDefault(new Key("root", ArrayIndexers.NoIndexers));
        await using var context = CreateContext();
        _rowCount = await context.Entries.CountAsync();
    }

    void Destroy() => _connection.Dispose();

    [Fact] void should_create_the_root_row() => _rowCount.ShouldEqual(1);
    [Fact] void should_find_the_root_row() => _result.ShouldNotBeNull();
    [Fact] void should_persist_the_count_on_the_root_from_key() => Convert.ToInt64(((IDictionary<string, object?>)_result!)["count"]).ShouldEqual(1L);

    ReadModelDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ReadModelDbContext>()
            .UseSqlite(_connection)
            .AddConceptAsSupport()
            .Options;
        return new ReadModelDbContext(options, "test", _columns, Substitute.For<IReadModelMigrator>());
    }
}
