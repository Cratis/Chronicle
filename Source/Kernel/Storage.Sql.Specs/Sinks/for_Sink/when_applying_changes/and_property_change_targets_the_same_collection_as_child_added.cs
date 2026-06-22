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
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SqlSink = Cratis.Chronicle.Storage.Sql.Sinks.Sink;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes;

public class and_property_change_targets_the_same_collection_as_child_added : Specification
{
    const string ContainerName = "test_read_models";

    /// <summary>
    /// The read-model schema names the columns the typed-column sink writes through. The sink
    /// derives its column list from this schema via ProjectedColumns.ForSchema; the spec passes
    /// the same derivation into ReadModelDbContext so EF builds an entity whose key and columns
    /// line up with what the sink is about to write.
    /// </summary>
    const string SchemaJson = """
        {
          "type": "object",
          "properties": {
            "name": { "type": "string" },
            "roles": { "type": "array", "items": { "type": "object" } }
          }
        }
        """;

    readonly EventStoreName _eventStoreName = "test-event-store";
    readonly EventStoreNamespaceName _namespace = "test-namespace";
    readonly JsonSchema _schema = JsonSchema.FromJson(SchemaJson);

    SqliteConnection _connection;
    SqlSink _sink;
    IDatabase _database;
    IChangeset<AppendedEvent, ExpandoObject> _changeset;
    Key _key;
    ExpandoObject? _result;
    IEnumerable<FailedPartition> _failedPartitions;
    IReadOnlyList<ProjectedColumn> _columns;

    void Establish()
    {
        _columns = ProjectedColumns.ForSchema(_schema);
        _key = new Key("user-1", ArrayIndexers.NoIndexers);
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using (var context = CreateContext())
        {
            context.Database.EnsureCreated();
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

        var role = CreateRole("Administrator");
        var rolesProperty = new PropertyPath("roles");
        var childAdded = new ChildAdded(role, rolesProperty, new PropertyPath("role"), "Administrator", ArrayIndexers.NoIndexers);

        dynamic state = new ExpandoObject();
        state.name = "Ada Lovelace";
        state.roles = new[] { role };

        PropertyDifference[] differences =
        [
            new PropertyDifference(new PropertyPath("name"), null, "Ada Lovelace"),
            new PropertyDifference(rolesProperty, Array.Empty<object>(), new[] { role })
        ];
        var propertiesChanged = new PropertiesChanged<ExpandoObject>(state, differences);

        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.InitialState.Returns(new ExpandoObject());
        Change[] changes = [propertiesChanged, childAdded];
        _changeset.Changes.Returns(changes);
    }

    async Task Because()
    {
        _failedPartitions = await _sink.ApplyChanges(_key, _changeset, 42UL);
        _result = await _sink.FindOrDefault(_key);
    }

    void Destroy() => _connection.Dispose();

    [Fact] void should_not_fail() => _failedPartitions.ShouldBeEmpty();
    [Fact] void should_keep_non_conflicting_property_changes() => GetValue<string>("name").ShouldEqual("Ada Lovelace");
    [Fact] void should_apply_the_child_operation_once() => GetRoles().Length.ShouldEqual(1);
    [Fact] void should_keep_the_added_role() => GetValue<string>(GetRoles()[0], "role").ShouldEqual("Administrator");

    T GetValue<T>(string property) => GetValue<T>(_result, property);

    static T GetValue<T>(ExpandoObject target, string property)
    {
        var dictionary = (IDictionary<string, object?>)target;
        return (T)dictionary[property]!;
    }

    ExpandoObject[] GetRoles()
    {
        var dictionary = (IDictionary<string, object?>)_result;
        return ((IEnumerable<object>)dictionary["roles"]).Cast<ExpandoObject>().ToArray();
    }

    ReadModelDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ReadModelDbContext>()
            .UseSqlite(_connection)
            .AddConceptAsSupport()
            .Options;

        return new ReadModelDbContext(options, ContainerName, _columns, Substitute.For<IReadModelMigrator>());
    }

    static ExpandoObject CreateRole(string role)
    {
        dynamic item = new ExpandoObject();
        item.role = role;
        return item;
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
