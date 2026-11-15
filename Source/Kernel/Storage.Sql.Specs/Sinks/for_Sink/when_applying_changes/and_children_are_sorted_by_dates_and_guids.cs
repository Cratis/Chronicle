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

public class and_children_are_sorted_by_dates_and_guids : Specification
{
    const string ContainerName = "test_read_models";

    const string SchemaJson = """
        {
          "type": "object",
          "properties": {
            "name": { "type": "string" },
            "visits": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "id": { "type": "string", "format": "guid" },
                  "occurred": { "type": "string", "format": "date-time" }
                }
              }
            },
            "links": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "id": { "type": "string" },
                  "reference": { "type": "string", "format": "guid" }
                }
              }
            }
          }
        }
        """;

    static readonly Guid _visitA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000000");
    static readonly Guid _visitB = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000000");
    static readonly Guid _visitC = Guid.Parse("cccccccc-0000-0000-0000-000000000000");
    static readonly Guid _visitD = Guid.Parse("11111111-0000-0000-0000-000000000000");

    static readonly ChildrenOrdering _byOccurred = new(ChildrenPlacement.Sorted, new("occurred"));
    static readonly ChildrenOrdering _byReference = new(ChildrenPlacement.Sorted, new("reference"));

    readonly JsonSchema _schema = JsonSchema.FromJson(SchemaJson);

    SqliteConnection _connection;
    SqlSink _sink;
    Key _key;
    IReadOnlyList<ProjectedColumn> _columns;
    ulong _sequenceNumber;
    string _afterEarlierDate;
    string _afterLaterInstantWithAnEarlierLocalDate;
    string _afterEqualInstant;
    string _afterSmallerGuid;

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

        await AddVisit(_visitB, new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero), null);
        await AddLink("x", Guid.Parse("22222222-0000-0000-0000-000000000000"), null);
    }

    async Task Because()
    {
        await AddVisit(_visitC, new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero), _byOccurred);
        _afterEarlierDate = await StoredIds("visits");

        // Its local date is earlier than the first visit's, but the instant is an hour later.
        await AddVisit(_visitA, new DateTimeOffset(2026, 1, 1, 23, 0, 0, TimeSpan.FromHours(-2)), _byOccurred);
        _afterLaterInstantWithAnEarlierLocalDate = await StoredIds("visits");

        // The same instant as the first visit, written with another offset - the identifier breaks the tie.
        await AddVisit(_visitD, new DateTimeOffset(2026, 1, 2, 2, 0, 0, TimeSpan.FromHours(2)), _byOccurred);
        _afterEqualInstant = await StoredIds("visits");

        await AddLink("y", Guid.Parse("11111111-0000-0000-0000-000000000000"), _byReference);
        _afterSmallerGuid = await StoredIds("links");
    }

    void Destroy() => _connection.Dispose();

    [Fact] void should_place_an_earlier_date_first() => _afterEarlierDate.ShouldEqual($"{_visitC},{_visitB}");
    [Fact] void should_order_dates_by_their_instant() => _afterLaterInstantWithAnEarlierLocalDate.ShouldEqual($"{_visitC},{_visitB},{_visitA}");
    [Fact] void should_order_equal_dates_by_the_guid_identifier() => _afterEqualInstant.ShouldEqual($"{_visitC},{_visitD},{_visitB},{_visitA}");
    [Fact] void should_order_by_a_guid_sort_value() => _afterSmallerGuid.ShouldEqual("y,x");

    Task AddVisit(Guid id, DateTimeOffset occurred, ChildrenOrdering? ordering) =>
        AddChild("visits", id, new Dictionary<string, object?> { ["id"] = id, ["occurred"] = occurred }, ordering);

    Task AddLink(string id, Guid reference, ChildrenOrdering? ordering) =>
        AddChild("links", id, new Dictionary<string, object?> { ["id"] = id, ["reference"] = reference }, ordering);

    async Task AddChild(string collection, object id, IDictionary<string, object?> values, ChildrenOrdering? ordering)
    {
        var child = new ExpandoObject();
        foreach (var (name, value) in values)
        {
            ((IDictionary<string, object?>)child)[name] = value;
        }

        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        Change[] changes = [new ChildAdded(child, new PropertyPath(collection), new PropertyPath("id"), id, ArrayIndexers.NoIndexers, ordering)];
        changeset.Changes.Returns(changes);
        await _sink.ApplyChanges(_key, changeset, _sequenceNumber++);
    }

    async Task<string> StoredIds(string collection)
    {
        await using var context = CreateContext();
        var stored = await context.Entries.AsNoTracking().SingleAsync();
        var children = JsonNode.Parse((string)stored[collection]!)!.AsArray();
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
