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
/// Verifies that a PropertiesChanged with leaf-relative property paths and collection-locating
/// ArrayIndexers correctly updates the target child element in a JSON column. This scenario
/// arises when the projection engine takes the "existing child" path in SetProperties — no
/// ChildAdded is emitted, only PropertiesChanged with diffs like {PropertyPath="intValue",
/// ArrayIndexers={children[stringValue=SecondKey]}}. The SQL sink must locate the JSON column
/// via the ArrayIndexers and apply the change within the identified element.
/// </summary>
public class and_property_change_has_leaf_relative_path_with_array_indexers_targeting_existing_child : Specification
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
                  "stringValue": { "type": "string" },
                  "intValue": { "type": "integer" }
                }
              }
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

        // First event: adds "TheKey" child with intValue=10.
        await ApplyChildAdded("TheKey", 10);

        // Second event: adds "SecondKey" child with intValue=20.
        await ApplyChildAdded("SecondKey", 20);
    }

    async Task Because()
    {
        // Third event: updates the existing "SecondKey" child (intValue 20 → 99).
        // The projection engine produces leaf-relative property paths with ArrayIndexers — this
        // exercises the TryApplyDifferenceViaArrayIndexers fallback in ApplySingleDifference.
        await ApplyLeafRelativePropertyChange("SecondKey", 99);

        _result = await _sink.FindOrDefault(_key);
    }

    void Destroy() => _connection.Dispose();

    [Fact] void should_find_the_parent() => _result.ShouldNotBeNull();
    [Fact] void should_have_two_children() => GetChildren().Length.ShouldEqual(2);
    [Fact] void should_not_modify_the_first_child_int_value() => GetIntValue(GetChild("TheKey")).ShouldEqual(10);
    [Fact] void should_update_the_second_child_int_value() => GetIntValue(GetChild("SecondKey")).ShouldEqual(99);

    async Task ApplyChildAdded(string stringValue, int intValue)
    {
        dynamic child = new ExpandoObject();
        child.stringValue = stringValue;
        child.intValue = intValue;

        var childrenPath = new PropertyPath("children");
        var childAdded = new ChildAdded(child, childrenPath, new PropertyPath("stringValue"), stringValue, ArrayIndexers.NoIndexers);

        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        Change[] changes = [childAdded];
        changeset.Changes.Returns(changes);

        await _sink.ApplyChanges(_key, changeset, (EventSequenceNumber)(ulong)intValue);
    }

    async Task ApplyLeafRelativePropertyChange(string targetStringValue, int newIntValue)
    {
        // Simulate the PropertiesChanged that the projection engine emits when SetProperties runs
        // for an existing child: the property path is leaf-relative ("intValue") and the
        // collection locator is carried exclusively by the ArrayIndexers.
        var childrenIndexer = new ArrayIndexer(new PropertyPath("children"), new PropertyPath("stringValue"), targetStringValue);
        var arrayIndexers = new ArrayIndexers([childrenIndexer]);

        PropertyDifference[] differences =
        [
            new PropertyDifference(new PropertyPath("intValue"), null, newIntValue, arrayIndexers)
        ];

        var state = new ExpandoObject();
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        Change[] changes = [new PropertiesChanged<ExpandoObject>(state, differences)];
        changeset.Changes.Returns(changes);

        await _sink.ApplyChanges(_key, changeset, 99UL);
    }

    T GetValue<T>(string property) => GetValue<T>(_result, property);

    static T GetValue<T>(IDictionary<string, object?> target, string property) => (T)target[property]!;

    static T GetValue<T>(ExpandoObject target, string property) => GetValue<T>((IDictionary<string, object?>)target, property);

    ExpandoObject[] GetChildren()
    {
        var dictionary = (IDictionary<string, object?>)_result;
        return ((IEnumerable<object>)dictionary["children"]).Cast<ExpandoObject>().ToArray();
    }

    ExpandoObject GetChild(string stringValue) =>
        GetChildren().First(c => GetValue<string>(c, "stringValue") == stringValue);

    static int GetIntValue(ExpandoObject child) =>
        Convert.ToInt32(((IDictionary<string, object?>)child)["intValue"]);

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
