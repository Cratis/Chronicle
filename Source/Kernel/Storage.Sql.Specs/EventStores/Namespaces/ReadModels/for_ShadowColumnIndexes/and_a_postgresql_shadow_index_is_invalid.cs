// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ShadowColumnIndexes;

[Collection(PostgreSqlCollection.Name)]
public class and_a_postgresql_shadow_index_is_invalid(PostgreSqlFixture fixture) : Specification
{
    readonly ILogger<ReadModelMigrator> _logger = Substitute.For<ILogger<ReadModelMigrator>>();
    PostgreSqlSinkHarness _harness;
    ReadModelDbContext _context;
    Exception _failure;
    string _failedIndex;
    string _warning = string.Empty;
    bool _wasInvalid;
    bool _lockReleased;
    int _invalidIndexes;
    int _validIndexes;
    int _oldIndexes;

    async Task Establish()
    {
        await PrepareTable();
        await TimeOutTheConcurrentBuild();
        await ProbeSchemaLock();
    }

    async Task Because()
    {
        if (_lockReleased)
        {
            await DatabaseSchemaLock.Run(_context, () => ShadowColumnIndexes.Ensure(_context));
            await DatabaseSchemaLock.Run(_context, () => ShadowColumnIndexes.Ensure(_context));
        }
        _invalidIndexes = await _context.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM pg_index x WHERE x.indrelid='shadow_repair'::regclass AND NOT x.indisvalid").SingleAsync();
        _validIndexes = await _context.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM pg_index x JOIN pg_class i ON i.oid=x.indexrelid WHERE x.indrelid='shadow_repair'::regclass AND x.indisvalid AND NOT x.indisunique AND i.relname NOT LIKE 'chronicle_imx_%'").SingleAsync();
        _oldIndexes = await _context.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM pg_indexes WHERE schemaname=current_schema() AND indexname={_failedIndex}").SingleAsync();
    }

    void Destroy()
    {
        _context.Dispose();
        _harness.Dispose();
    }

    [Fact] void should_surface_the_timeout_or_cancellation() => (_failure is OperationCanceledException or NpgsqlException).ShouldBeTrue();
    [Fact] void should_leave_an_invalid_index_after_the_timed_out_concurrent_build() => _wasInvalid.ShouldBeTrue();
    [Fact] void should_log_the_table_name() => _warning.ShouldContain("shadow_repair");
    [Fact] void should_log_the_failed_index_name() => _warning.ShouldContain(_failedIndex);
    [Fact] void should_release_the_database_wide_schema_lock() => _lockReleased.ShouldBeTrue();
    [Fact] void should_drop_the_failed_build_on_the_next_ensure() => _oldIndexes.ShouldEqual(0);
    [Fact] void should_leave_no_invalid_index() => _invalidIndexes.ShouldEqual(0);
    [Fact] void should_rebuild_one_valid_index_idempotently() => _validIndexes.ShouldEqual(1);

    async Task PrepareTable()
    {
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
        _logger.WhenForAnyArgs(logger => logger.Log(default, default, default(object), default, default!)).Do(call => _warning = call[2]!.ToString()!);
        _harness = new PostgreSqlSinkHarness { Fixture = fixture };
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"object","properties":{"part":{"type":"string"}}}}}""");
        _harness.CreateSink(new ReadModelDefinition("repair", "shadow_repair", "Repair", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "repair", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
        var options = SpecDbContextOptions.Kernel<ReadModelDbContext>(_harness.ConnectionString);
        var migrator = new ReadModelMigrator(new TableMigrator<ReadModelDbContext>(Substitute.For<ILogger<TableMigrator<ReadModelDbContext>>>()), _logger);
        _context = new ReadModelDbContext(options, "shadow_repair", ProjectedColumns.ForSchema(schema), migrator);
        await _context.EnsureTableExists();
        await _context.Database.ExecuteSqlRawAsync("INSERT INTO shadow_repair (id, \"__compositeKey\") VALUES ('legacy', '')");
        var validName = await _context.Database.SqlQuery<string>($"SELECT i.relname AS \"Value\" FROM pg_index x JOIN pg_class i ON i.oid=x.indexrelid WHERE x.indrelid='shadow_repair'::regclass AND x.indisvalid AND NOT x.indisunique AND i.relname NOT LIKE 'chronicle_imx_%'").SingleAsync();
        await ReadModelIndexOperations.Drop(_context, _context.TableName, validName, online: true);
    }

    async Task TimeOutTheConcurrentBuild()
    {
        await using var writer = new NpgsqlConnection(_harness.ConnectionString);
        await writer.OpenAsync();
        await using var transaction = await writer.BeginTransactionAsync();
        await using var command = writer.CreateCommand();
        command.CommandText = "UPDATE shadow_repair SET \"__compositeKey\" = '' WHERE id='legacy'";
        await command.ExecuteNonQueryAsync();

        // CONCURRENTLY registers the invalid index, then waits for this uncommitted writer before scanning.
        _failure = await Catch.Exception(() => DatabaseSchemaLock.Run(_context, () => ShadowColumnIndexes.Ensure(_context, new() { ShadowIndexDdlTimeoutSeconds = 5 }, _logger)));
        var invalid = await _context.Database.SqlQuery<string>($"SELECT i.relname AS \"Value\" FROM pg_index x JOIN pg_class i ON i.oid=x.indexrelid WHERE x.indrelid='shadow_repair'::regclass AND NOT x.indisvalid").ToArrayAsync();
        _wasInvalid = invalid.Length == 1;
        _failedIndex = invalid.SingleOrDefault() ?? string.Empty;
        await transaction.RollbackAsync();
    }

    async Task ProbeSchemaLock()
    {
        await using var probe = new NpgsqlConnection(_harness.ConnectionString);
        await probe.OpenAsync();
        var resource = DatabaseSchemaLock.ResourceFor(probe.Database);
        _lockReleased = await Catch.Exception(() => PostgreSqlDatabaseSchemaLock.Instance.Acquire(probe, resource, TimeSpan.Zero)) is null;
        if (_lockReleased)
        {
            await PostgreSqlDatabaseSchemaLock.Instance.Release(probe, resource);
        }
    }
}
