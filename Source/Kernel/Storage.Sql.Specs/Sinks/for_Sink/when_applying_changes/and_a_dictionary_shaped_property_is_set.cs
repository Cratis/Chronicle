// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
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
/// including object-keyed input dictionaries. Readback uses ordinal string keys, preserving JSON
/// property names without requiring the same concrete dictionary type as the input.
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
              "additionalProperties": {}
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
    JsonNode _storedEntries;
    JsonObject _serializedReadback;
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
        // Keep object-keyed input: recognizing only string-keyed dictionaries caused issue 3568.
        var entries = new Dictionary<object, object?>
        {
            ["first"] = "firstValue",
            ["second"] = "secondValue",
            ["First"] = "distinctCaseValue",
            ["optional"] = null,
            ["nested"] = new Dictionary<string, object?>
            {
                ["missing"] = null,
                ["name"] = "nestedValue"
            }
        };

        var state = new ExpandoObject();
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        Change[] changes = [new PropertiesChanged<ExpandoObject>(state, [new PropertyDifference(new PropertyPath("entries"), null, entries)])];
        changeset.Changes.Returns(changes);

        await _sink.ApplyChanges(_key, changeset, EventSequenceNumber.First);

        _result = await _sink.FindOrDefault(_key);
        _serializedReadback = new ExpandoObjectConverter(new TypeFormats()).ToJsonObject(_result!, _schema);

        await using var context = CreateContext();
        var stored = await context.Entries.AsNoTracking().SingleAsync();
        _storedEntries = JsonNode.Parse((string)stored["entries"]!)!;
    }

    void Destroy() => _connection.Dispose();

    [Fact] void should_find_the_read_model() => _result.ShouldNotBeNull();
    [Fact] void should_persist_a_json_object_instead_of_key_value_pairs() => _storedEntries.ShouldBeOfExactType<JsonObject>();
    [Fact] void should_preserve_the_exact_stored_keys() => _storedEntries.AsObject().Select(entry => entry.Key).ShouldContainOnly(["first", "second", "First", "optional", "nested"]);
    [Fact] void should_return_a_dictionary_with_the_exact_logical_keys() => GetEntries().Keys.ShouldContainOnly(["first", "second", "First", "optional", "nested"]);
    [Fact] void should_preserve_the_first_entry() => GetEntries()["first"].ShouldEqual("firstValue");
    [Fact] void should_preserve_the_second_entry() => GetEntries()["second"].ShouldEqual("secondValue");
    [Fact] void should_preserve_the_distinct_case_entry() => GetEntries()["First"].ShouldEqual("distinctCaseValue");
    [Fact] void should_keep_key_lookup_case_sensitive() => GetEntries().ContainsKey("FIRST").ShouldBeFalse();
    [Fact] void should_preserve_a_null_dictionary_value() => GetEntries()["optional"].ShouldBeNull();
    [Fact] void should_retain_the_nested_null_key() => GetNestedEntries().ContainsKey("missing").ShouldBeTrue();
    [Fact] void should_preserve_the_nested_null_value() => GetNestedEntries()["missing"].ShouldBeNull();
    [Fact] void should_preserve_the_nested_non_null_value() => GetNestedEntries()["name"].ShouldEqual("nestedValue");
    [Fact] void should_not_add_json_quotes_to_keys_after_readback() => _serializedReadback["entries"]!.AsObject().Select(entry => entry.Key).ShouldContainOnly(["first", "second", "First", "optional", "nested"]);
    [Fact] void should_serialize_the_readback_without_changing_its_json_content() => JsonNode.DeepEquals(_storedEntries, _serializedReadback["entries"]).ShouldBeTrue();

    IDictionary<string, object?> GetEntries() => (IDictionary<string, object?>)((IDictionary<string, object?>)_result!)["entries"]!;

    IDictionary<string, object?> GetNestedEntries() => (IDictionary<string, object?>)GetEntries()["nested"]!;

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
