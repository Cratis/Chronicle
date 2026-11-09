// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Arc.Connections;
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

public class and_ordered_children_are_added : Specification
{
    const string ContainerName = "test_read_models";

    const string SchemaJson = """
        {
          "type": "object",
          "properties": {
            "name": { "type": "string" },
            "children": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "id": { "type": "string" },
                  "rank": { "type": "integer" }
                }
              }
            }
          }
        }
        """;

    readonly JsonSchema _schema = JsonSchema.FromJson(SchemaJson);

    SqliteConnection _connection;
    SqlSink _sink;
    Key _key;
    IReadOnlyList<ProjectedColumn> _columns;
    string _afterPrepend;
    string _afterSort;

    async Task Establish()
    {
        _columns = ProjectedColumns.ForSchema(_schema);
        _key = new Key("parent-1", ArrayIndexers.NoIndexers);
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        await using (var context = CreateContext())
        {
            await context.Database.EnsureCreatedAsync();
        }

        var database = Substitute.For<IDatabase>();
        database.ReadModelTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ProjectedColumn>>(), Arg.Any<ConnectionPurpose>())
            .Returns(_ => Task.FromResult(new DbContextScope<ReadModelDbContext>(CreateContext(), () => { })));

        _sink = new SqlSink(
            "test-event-store",
            "test-namespace",
            CreateReadModelDefinition(),
            database,
            new ExpandoObjectConverter(new TypeFormats()),
            new ReplayingTables());

        await AddChild("b", 2, null, EventSequenceNumber.First);
    }

    async Task Because()
    {
        await AddChild("a", 1, new(ChildrenPlacement.Prepend, PropertyPath.NotSet), 1UL);
        _afterPrepend = await StoredIds();
        await AddChild("c", 3, new(ChildrenPlacement.Sorted, new("rank"), SortDirection.Descending), 2UL);
        _afterSort = await StoredIds();
    }

    void Destroy() => _connection.Dispose();

    [Fact] void should_insert_a_prepended_child_first() => _afterPrepend.ShouldEqual("a,b");
    [Fact] void should_order_the_collection_by_the_sort_property_after_a_sorted_insert() => _afterSort.ShouldEqual("c,b,a");

    async Task AddChild(string id, int rank, ChildrenOrdering? ordering, EventSequenceNumber sequenceNumber)
    {
        var child = new ExpandoObject();
        ((IDictionary<string, object?>)child)["id"] = id;
        ((IDictionary<string, object?>)child)["rank"] = rank;
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        Change[] changes = [new ChildAdded(child, new PropertyPath("children"), new PropertyPath("id"), id, ArrayIndexers.NoIndexers, ordering)];
        changeset.Changes.Returns(changes);
        await _sink.ApplyChanges(_key, changeset, sequenceNumber);
    }

    async Task<string> StoredIds()
    {
        await using var context = CreateContext();
        var stored = await context.Entries.AsNoTracking().SingleAsync();
        var children = JsonNode.Parse((string)stored["children"]!)!.AsArray();
        return string.Join(',', children.Select(_ => _!["id"]!.GetValue<string>()));
    }

    ReadModelDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ReadModelDbContext>()
            .UseSqlite(_connection)
            .AddConceptAsSupport().WithoutServiceProviderCaching()
            .Options;

        return new ReadModelDbContext(options, ContainerName, _columns, Substitute.For<IReadModelMigrator>());
    }

    ReadModelDefinition CreateReadModelDefinition() =>
        new(
            "test-read-model",
            "TestReadModel",
            ContainerName,
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, _schema }
            },
            []);
}
