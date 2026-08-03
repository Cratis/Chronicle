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

/// <summary>
/// Regression coverage for https://github.com/Cratis/Chronicle/issues/3568 - a dictionary-shaped
/// (additionalProperties) property used to be serialized as a JSON array of { "Key", "Value" }
/// objects instead of a JSON object, because UnwrapForJson only recognized IDictionary&lt;string,
/// object?&gt; and fell through to its generic IEnumerable branch for any other dictionary type -
/// including the Dictionary&lt;object, object&gt; that a schema-defined dictionary property round-trips
/// as.
/// </summary>
public class and_a_dictionary_shaped_property_is_set : Specification
{
    const string ContainerName = "test_read_models";

    const string SchemaJson = """
        {
          "type": "object",
          "properties": {
            "name": { "type": "string" },
            "entries": {
              "type": "object",
              "additionalProperties": { "type": "string" }
            }
          }
        }
        """;

    readonly EventStoreName _eventStoreName = "test-event-store";
    readonly EventStoreNamespaceName _namespace = "test-namespace";
    readonly JsonSchema _schema = JsonSchema.FromJson(SchemaJson);

    SqliteConnection _connection;
    SqlSink _sink;
    IDatabase _database;
    Key _key;
    ExpandoObject? _result;
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

        _database = Substitute.For<IDatabase>();
        _database.ReadModelTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ProjectedColumn>>())
            .Returns(_ => Task.FromResult(new DbContextScope<ReadModelDbContext>(CreateContext(), () => { })));

        _sink = new SqlSink(
            _eventStoreName,
            _namespace,
            CreateReadModelDefinition(),
            _database,
            new ExpandoObjectConverter(new TypeFormats()));
    }

    async Task Because()
    {
        var entries = new Dictionary<object, object>
        {
            ["first"] = "firstValue",
            ["second"] = "secondValue"
        };

        var state = new ExpandoObject();
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        Change[] changes = [new PropertiesChanged<ExpandoObject>(state, [new PropertyDifference(new PropertyPath("entries"), null, entries)])];
        changeset.Changes.Returns(changes);

        await _sink.ApplyChanges(_key, changeset, EventSequenceNumber.First);

        _result = await _sink.FindOrDefault(_key);
    }

    void Destroy() => _connection.Dispose();

    [Fact] void should_find_the_read_model() => _result.ShouldNotBeNull();
    [Fact] void should_store_entries_as_a_dictionary() => GetEntries().ShouldBeOfExactType<Dictionary<object, object>>();
    [Fact] void should_preserve_the_first_entry() => GetEntries()["first"].ShouldEqual("firstValue");
    [Fact] void should_preserve_the_second_entry() => GetEntries()["second"].ShouldEqual("secondValue");

    Dictionary<object, object> GetEntries() => (Dictionary<object, object>)((IDictionary<string, object?>)_result!)["entries"]!;

    ReadModelDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ReadModelDbContext>()
            .UseSqlite(_connection)
            .AddConceptAsSupport()
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
