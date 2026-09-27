// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
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
using Microsoft.EntityFrameworkCore;
using SqlSink = Cratis.Chronicle.Storage.Sql.Sinks.Sink;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_observing_instances.given;

/// <summary>
/// A real SQL read model whose writes use the same shared-type entity as a projection.
/// </summary>
public abstract class a_sql_read_model : Specification
{
    const string ContainerName = "observed_read_models";
    const string SchemaJson = """
        {
          "type": "object",
          "properties": {
            "id": { "type": "string", "format": "uuid" },
            "name": { "type": "string" }
          }
        }
        """;

    protected SqlSink _sink;
    protected readonly Guid _firstId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    protected readonly Guid _secondId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    protected readonly TaskCompletionSource<IEnumerable<ExpandoObject>> _initial = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource<IEnumerable<ExpandoObject>> _updated = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected IDisposable _subscription;
    protected IEnumerable<ExpandoObject> _firstPage;
    protected IEnumerable<ExpandoObject> _secondPage;

    DbConnection _connection;
    protected IDatabase _database;
    IReadOnlyList<ProjectedColumn> _columns;
    int _emissions;

    protected abstract DbConnection CreateConnection();
    protected abstract void Configure(DbContextOptionsBuilder<ReadModelDbContext> options, DbConnection connection);

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync(SchemaJson);
        _columns = ProjectedColumns.ForSchema(schema);
        _connection = CreateConnection();
        await _connection.OpenAsync();
        await using (var context = CreateContext())
        {
            await context.Database.EnsureCreatedAsync();
        }

        _database = Substitute.For<IDatabase>();
        _database.LiveQueryPollingInterval.Returns(TimeSpan.FromMilliseconds(50));
        _database.ReadModelTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ProjectedColumn>>())
            .Returns(_ => Task.FromResult(new DbContextScope<ReadModelDbContext>(CreateContext(), () => { })));
        _sink = new SqlSink(
            "test-event-store",
            "test-namespace",
            new ReadModelDefinition(
                "test-read-model",
                ContainerName,
                "TestReadModel",
                ReadModelOwner.Client,
                ReadModelSource.Code,
                ReadModelObserverType.Projection,
                ReadModelObserverIdentifier.Unspecified,
                SinkDefinition.None,
                new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema },
                []),
            _database,
            new ExpandoObjectConverter(new TypeFormats()));
    }

    protected async Task Write(Guid id, string name)
    {
        var state = new ExpandoObject();
        Change[] changes = [new PropertiesChanged<ExpandoObject>(state, [new PropertyDifference(new PropertyPath("name"), null, name)])];
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns(changes);
        await _sink.ApplyChanges(new Key(id, ArrayIndexers.NoIndexers), changeset, EventSequenceNumber.First);
    }

    protected async Task ObservePagedWrite()
    {
        await Write(_firstId, "first");
        await Write(_secondId, "before");
        Subscribe(skip: 1, take: 1);
        _firstPage = await _initial.Task.WaitAsync(TimeSpan.FromSeconds(15));
        await Write(_secondId, "after");
        _secondPage = await _updated.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    protected void Subscribe(int skip = 0, int take = 50) => _subscription = _sink.ObserveInstances(skip: skip, take: take).Subscribe(
        instances =>
        {
            var snapshot = instances.ToArray();
            if (Interlocked.Increment(ref _emissions) == 1)
            {
                _initial.TrySetResult(snapshot);
            }
            else
            {
                _updated.TrySetResult(snapshot);
            }
        },
        error =>
        {
            _initial.TrySetException(error);
            _updated.TrySetException(error);
        });

    protected static string NameOf(IEnumerable<ExpandoObject> instances) =>
        (string)((IDictionary<string, object?>)instances.Single())["name"]!;

    void Destroy()
    {
        _subscription?.Dispose();
        _connection?.Dispose();
    }

    ReadModelDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ReadModelDbContext>();
        Configure(options, _connection);
        return new ReadModelDbContext(options.AddConceptAsSupport().Options, ContainerName, _columns, Substitute.For<IReadModelMigrator>());
    }
}
