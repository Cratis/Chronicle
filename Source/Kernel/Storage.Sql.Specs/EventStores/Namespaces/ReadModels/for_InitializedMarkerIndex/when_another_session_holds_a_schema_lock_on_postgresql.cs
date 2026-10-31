// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_InitializedMarkerIndex;

[Collection(PostgreSqlCollection.Name)]
public class when_another_session_holds_a_schema_lock_on_postgresql(PostgreSqlFixture fixture) : Specification
{
    const string TableName = "marker_lock_scope";
    readonly ILogger<ReadModelMigrator> _logger = Substitute.For<ILogger<ReadModelMigrator>>();
    PostgreSqlSinkHarness _harness;
    ReadModelDbContext _context;
    bool _builtWhileDatabaseLockHeld;
    int _indexesWhileDatabaseLockHeld;
    bool _builtWhileTableLockHeld;
    int _indexesWhileTableLockHeld;

    Task<int> MarkerIndexes() => _context.Database.SqlQuery<int>(
        $"SELECT COUNT(*)::int AS \"Value\" FROM pg_indexes WHERE schemaname=current_schema() AND tablename={TableName} AND left(indexname, 14)={InitializedMarkerIndex.Prefix}").SingleAsync();

    ReadModelIndexOperations.Execution Execution => new(Online: true, CommandTimeout: 2, Logger: _logger);

    async Task Establish()
    {
        _harness = new PostgreSqlSinkHarness { Fixture = fixture };
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"status":{"type":"string"}}}""");
        _harness.CreateSink(new ReadModelDefinition("marker", TableName, "Marker", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "marker", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
        var options = new DbContextOptionsBuilder<ReadModelDbContext>().UseNpgsql(_harness.ConnectionString).Options;
        var migrator = new ReadModelMigrator(new TableMigrator<ReadModelDbContext>(Substitute.For<ILogger<TableMigrator<ReadModelDbContext>>>()), _logger);
        _context = new ReadModelDbContext(options, TableName, ProjectedColumns.ForSchema(schema), migrator);
        await _context.EnsureTableExists();
        await DropMarkerIndexes();
    }

    async Task Because()
    {
        await using (await Hold(DatabaseSchemaLock.ResourceFor(_context.Database.GetDbConnection().Database)))
        {
            _builtWhileDatabaseLockHeld = await InitializedMarkerIndex.Ensure(_context, Execution, TimeProvider.System).WaitAsync(TimeSpan.FromSeconds(30));
            _indexesWhileDatabaseLockHeld = await MarkerIndexes();
        }

        await DropMarkerIndexes();
        await using (await Hold($"chronicle:read-model-marker-index:{TableName}"))
        {
            _builtWhileTableLockHeld = await InitializedMarkerIndex.Ensure(_context, Execution, TimeProvider.System).WaitAsync(TimeSpan.FromSeconds(30));
            _indexesWhileTableLockHeld = await MarkerIndexes();
        }
    }

    void Destroy()
    {
        _context.Dispose();
        _harness.Dispose();
    }

    [Fact] void should_build_without_the_database_wide_schema_lock() => _builtWhileDatabaseLockHeld.ShouldBeTrue();
    [Fact] void should_create_the_marker_index_while_the_database_lock_is_held() => _indexesWhileDatabaseLockHeld.ShouldEqual(1);
    [Fact] void should_defer_while_another_kernel_holds_the_table_marker_lock() => _builtWhileTableLockHeld.ShouldBeFalse();
    [Fact] void should_not_create_the_index_while_deferred() => _indexesWhileTableLockHeld.ShouldEqual(0);

    async Task DropMarkerIndexes()
    {
        var names = await _context.Database.SqlQuery<string>(
            $"SELECT indexname AS \"Value\" FROM pg_indexes WHERE schemaname=current_schema() AND tablename={TableName} AND left(indexname, 14)={InitializedMarkerIndex.Prefix}").ToArrayAsync();
        foreach (var name in names)
        {
            await ReadModelIndexOperations.Drop(_context, TableName, name, online: true);
        }
    }

    async Task<HeldLock> Hold(string resource)
    {
        var connection = new NpgsqlConnection(_harness.ConnectionString);
        await connection.OpenAsync();
        await PostgreSqlDatabaseSchemaLock.Instance.Acquire(connection, resource, TimeSpan.Zero);
        return new HeldLock(connection, resource);
    }

    sealed class HeldLock(NpgsqlConnection connection, string resource) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await PostgreSqlDatabaseSchemaLock.Instance.Release(connection, resource);
            await connection.DisposeAsync();
        }
    }
}
