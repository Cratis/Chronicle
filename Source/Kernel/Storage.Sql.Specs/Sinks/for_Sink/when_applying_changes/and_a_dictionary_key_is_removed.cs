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

public class and_a_dictionary_key_is_removed : Specification
{
    const string ContainerName = "test_read_models";

    const string SchemaJson = """
        {
          "type": "object",
          "properties": {
            "name": { "type": "string" },
            "slots": {
              "type": "object",
              "additionalProperties": { "type": "string" }
            }
          }
        }
        """;

    readonly JsonSchema _schema = JsonSchema.FromJson(SchemaJson);

    SqliteConnection _connection;
    SqlSink _sink;
    Key _key;
    JsonObject _storedSlots;
    IReadOnlyList<ProjectedColumn> _columns;

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

        var slots = new Dictionary<string, object?>
        {
            ["first"] = "series-1",
            ["second"] = "series-2"
        };
        await Apply(new PropertyDifference(new PropertyPath("slots"), null, slots), EventSequenceNumber.First);
    }

    async Task Because()
    {
        await Apply(PropertyDifference.Removal(new PropertyPath("slots.first"), "series-1"), 1UL);

        await using var context = CreateContext();
        var stored = await context.Entries.AsNoTracking().SingleAsync();
        _storedSlots = JsonNode.Parse((string)stored["slots"]!)!.AsObject();
    }

    void Destroy() => _connection.Dispose();

    [Fact] void should_remove_the_key_rather_than_store_null() => _storedSlots.ContainsKey("first").ShouldBeFalse();
    [Fact] void should_keep_the_other_key() => _storedSlots["second"]!.GetValue<string>().ShouldEqual("series-2");

    async Task Apply(PropertyDifference difference, EventSequenceNumber sequenceNumber)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        Change[] changes = [new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [difference])];
        changeset.Changes.Returns(changes);
        await _sink.ApplyChanges(_key, changeset, sequenceNumber);
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
