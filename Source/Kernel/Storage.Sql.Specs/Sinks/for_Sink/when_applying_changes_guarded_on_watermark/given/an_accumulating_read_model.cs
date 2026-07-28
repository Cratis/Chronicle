// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
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

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_guarded_on_watermark.given;

public class an_accumulating_read_model : Specification
{
    protected const string ContainerName = "test_read_models";

    const string SchemaJson = """
        {
          "type": "object",
          "properties": {
            "count": { "type": "integer" }
          }
        }
        """;

    readonly EventStoreName _eventStoreName = "test-event-store";
    readonly EventStoreNamespaceName _namespace = "test-namespace";
    readonly JsonSchema _schema = JsonSchema.FromJson(SchemaJson);

    SqliteConnection _connection;
    IReadOnlyList<ProjectedColumn> _columns;

    protected SqlSink _sink;
    protected Key _key;

    void Establish()
    {
        _columns = ProjectedColumns.ForSchema(_schema);
        _key = new Key("counter-1", ArrayIndexers.NoIndexers);
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using (var context = CreateContext())
        {
            context.Database.EnsureCreated();
        }

        var database = Substitute.For<IDatabase>();
        database.ReadModelTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ProjectedColumn>>())
            .Returns(_ => Task.FromResult(new DbContextScope<ReadModelDbContext>(CreateContext(), () => { })));

        _sink = new SqlSink(
            _eventStoreName,
            _namespace,
            CreateReadModelDefinition(),
            database,
            new ExpandoObjectConverter(new TypeFormats()));
    }

    void Destroy() => _connection.Dispose();

    protected static IChangeset<AppendedEvent, ExpandoObject> ChangesetSettingCountTo(int count)
    {
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["count"] = count;

        PropertyDifference[] differences = [new PropertyDifference(new PropertyPath("count"), null, count)];
        var propertiesChanged = new PropertiesChanged<ExpandoObject>(state, differences);

        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        Change[] changes = [propertiesChanged];
        changeset.Changes.Returns(changes);
        return changeset;
    }

    protected async Task<int> CurrentCount()
    {
        var instance = await _sink.FindOrDefault(_key);
        return Convert.ToInt32(((IDictionary<string, object?>)instance)["count"], CultureInfo.InvariantCulture);
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
