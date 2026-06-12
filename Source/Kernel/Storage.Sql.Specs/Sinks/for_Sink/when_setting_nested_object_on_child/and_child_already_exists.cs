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

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_setting_nested_object_on_child;

public class and_child_already_exists : Specification
{
    const string ContainerName = "test_features";

    /// <summary>
    /// Mirrors the FeatureReadModel from the failing integration test
    /// when_projecting_with_nested_in_children: Feature has Name, an array
    /// of Slices, where each Slice has Id / Name / nested Command object
    /// (Command has Name and Schema).
    /// </summary>
    const string SchemaJson = """
        {
          "type": "object",
          "properties": {
            "Name": { "type": "string" },
            "Slices": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "Id": { "type": "string", "format": "guid" },
                  "Name": { "type": "string" },
                  "Command": {
                    "type": "object",
                    "properties": {
                      "Name": { "type": "string" },
                      "Schema": { "type": "string" }
                    }
                  }
                }
              }
            }
          }
        }
        """;

    static readonly Guid _featureIdValue = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    static readonly Guid _sliceIdValue = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");

    readonly EventStoreName _eventStoreName = "test-event-store";
    readonly EventStoreNamespaceName _namespace = "test-namespace";
    readonly JsonSchema _schema = JsonSchema.FromJson(SchemaJson);

    SqliteConnection _connection;
    SqlSink _sink;
    IDatabase _database;
    Key _featureKey;
    ExpandoObject? _result;
    IReadOnlyList<ProjectedColumn> _columns;

    async Task Establish()
    {
        _columns = ProjectedColumns.ForSchema(_schema);
        _featureKey = new Key(_featureIdValue.ToString(), ArrayIndexers.NoIndexers);
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

        // 1) FeatureCreated — root projection sets Name + initial-state for Slices.
        await ApplyFeatureCreated();

        // 2) SliceAddedToFeature — ChildAdded for Slices on the feature key, indexed by SliceId.
        await ApplySliceAdded();
    }

    async Task Because()
    {
        // 3) CommandSetOnSlice — sets the nested Command properties on the existing slice.
        await ApplyCommandSet();

        _result = await _sink.FindOrDefault(_featureKey);
    }

    void Destroy() => _connection.Dispose();

    [Fact] void should_find_the_feature() => _result.ShouldNotBeNull();
    [Fact] void should_keep_the_feature_name() => GetValue<string>("Name").ShouldEqual("My Feature");
    [Fact] void should_keep_one_slice() => GetSlices().Length.ShouldEqual(1);
    [Fact] void should_keep_the_slice_name() => GetValue<string>(GetSlices()[0], "Name").ShouldEqual("My Slice");

    [Fact]
    void should_have_a_nested_command_on_the_slice() =>
        GetCommand().ShouldNotBeNull();

    [Fact]
    void should_set_the_command_name() =>
        GetValue<string>(GetCommand(), "Name").ShouldEqual("Register");

    [Fact]
    void should_set_the_command_schema() =>
        GetValue<string>(GetCommand(), "Schema").ShouldEqual("{}");

    async Task ApplyFeatureCreated()
    {
        dynamic state = new ExpandoObject();
        state.Name = "My Feature";

        PropertyDifference[] differences =
        [
            new PropertyDifference(new PropertyPath("Name"), null, "My Feature")
        ];
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        Change[] changes = [new PropertiesChanged<ExpandoObject>(state, differences)];
        changeset.Changes.Returns(changes);

        await _sink.ApplyChanges(_featureKey, changeset, 1UL);
    }

    async Task ApplySliceAdded()
    {
        dynamic slice = new ExpandoObject();
        slice.Id = _sliceIdValue;
        slice.Name = "My Slice";

        var slicesPath = new PropertyPath("Slices");
        var childAdded = new ChildAdded(slice, slicesPath, new PropertyPath("Id"), _sliceIdValue, ArrayIndexers.NoIndexers);

        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        Change[] changes = [childAdded];
        changeset.Changes.Returns(changes);

        await _sink.ApplyChanges(_featureKey, changeset, 2UL);
    }

    async Task ApplyCommandSet()
    {
        // The projection engine emits a PropertiesChanged whose property paths address the nested
        // Command beneath the slice element selected by an ArrayIndexer.
        var sliceIndexer = new ArrayIndexer(new PropertyPath("Slices"), new PropertyPath("Id"), _sliceIdValue);
        var arrayIndexers = new ArrayIndexers([sliceIndexer]);

        var state = new ExpandoObject();
        var commandNamePath = new PropertyPath("Slices.Command.Name");
        var commandSchemaPath = new PropertyPath("Slices.Command.Schema");
        commandNamePath.SetValue(state, "Register", arrayIndexers);
        commandSchemaPath.SetValue(state, "{}", arrayIndexers);

        PropertyDifference[] differences =
        [
            new PropertyDifference(commandNamePath, null, "Register", arrayIndexers),
            new PropertyDifference(commandSchemaPath, null, "{}", arrayIndexers)
        ];
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        Change[] changes = [new PropertiesChanged<ExpandoObject>(state, differences)];
        changeset.Changes.Returns(changes);

        await _sink.ApplyChanges(_featureKey, changeset, 3UL);
    }

    T GetValue<T>(string property) => GetValue<T>(_result, property);

    static T GetValue<T>(IDictionary<string, object?> target, string property) => (T)target[property]!;

    static T GetValue<T>(ExpandoObject target, string property) => GetValue<T>((IDictionary<string, object?>)target, property);

    ExpandoObject[] GetSlices()
    {
        var dictionary = (IDictionary<string, object?>)_result;
        return ((IEnumerable<object>)dictionary["Slices"]).Cast<ExpandoObject>().ToArray();
    }

    ExpandoObject? GetCommand()
    {
        var slice = (IDictionary<string, object?>)GetSlices()[0];
        return slice.TryGetValue("Command", out var value) ? value as ExpandoObject : null;
    }

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
            "test-feature-read-model",
            "TestFeatureReadModel",
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
