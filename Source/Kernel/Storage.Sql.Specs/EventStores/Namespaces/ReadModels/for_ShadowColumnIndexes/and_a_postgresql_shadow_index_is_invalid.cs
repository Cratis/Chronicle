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
    PostgreSqlSinkHarness _harness;
    ReadModelDbContext _context;
    bool _wasInvalid;
    int _invalidIndexes;
    int _validIndexes;
    int _oldIndexes;

    async Task Establish()
    {
        _harness = new PostgreSqlSinkHarness { Fixture = fixture };
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"object","properties":{"part":{"type":"string"}}}}}""");
        _harness.CreateSink(new ReadModelDefinition("repair", "shadow_repair", "Repair", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "repair", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
        var options = new DbContextOptionsBuilder<ReadModelDbContext>().UseNpgsql(_harness.ConnectionString).Options;
        var migrator = new ReadModelMigrator(new TableMigrator<ReadModelDbContext>(Substitute.For<ILogger<TableMigrator<ReadModelDbContext>>>()), Substitute.For<ILogger<ReadModelMigrator>>());
        _context = new ReadModelDbContext(options, "shadow_repair", ProjectedColumns.ForSchema(schema), migrator);
        await _context.EnsureTableExists();
        await _context.Database.ExecuteSqlRawAsync("INSERT INTO shadow_repair (id, \"__compositeKey\") VALUES ('legacy', '')");

        // A failing predicate leaves a nonunique, single-column INVALID index, just like a cancelled concurrent build.
        try
        {
            await _context.Database.ExecuteSqlRawAsync("CREATE INDEX CONCURRENTLY chronicle_shx_invalid ON shadow_repair (\"__compositeKey\") WHERE 1 / length(\"__compositeKey\") > 0");
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.DivisionByZero)
        {
            _wasInvalid = await _context.Database.SqlQuery<bool>($"SELECT NOT x.indisvalid AS \"Value\" FROM pg_index x JOIN pg_class i ON i.oid=x.indexrelid WHERE i.relname='chronicle_shx_invalid' AND i.relnamespace=(SELECT oid FROM pg_namespace WHERE nspname=current_schema())").SingleAsync();
        }

        // Retain only the failed build, so reconciliation must not adopt it as the expected column's index.
        var validName = await _context.Database.SqlQuery<string>($"SELECT i.relname AS \"Value\" FROM pg_index x JOIN pg_class i ON i.oid=x.indexrelid WHERE x.indrelid='shadow_repair'::regclass AND x.indisvalid AND NOT x.indisunique").SingleAsync();
        await ReadModelIndexOperations.Drop(_context, _context.TableName, validName, online: true);
    }

    async Task Because()
    {
        await ShadowColumnIndexes.Ensure(_context);
        await ShadowColumnIndexes.Ensure(_context);
        _invalidIndexes = await _context.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM pg_index x JOIN pg_class i ON i.oid=x.indexrelid WHERE x.indrelid='shadow_repair'::regclass AND NOT x.indisvalid").SingleAsync();
        _validIndexes = await _context.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM pg_index x JOIN pg_class i ON i.oid=x.indexrelid WHERE x.indrelid='shadow_repair'::regclass AND x.indisvalid AND NOT x.indisunique").SingleAsync();
        _oldIndexes = await _context.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM pg_indexes WHERE schemaname=current_schema() AND indexname='chronicle_shx_invalid'").SingleAsync();
    }

    void Destroy()
    {
        _context.Dispose();
        _harness.Dispose();
    }

    [Fact] void should_have_a_failed_concurrent_build_to_repair() => _wasInvalid.ShouldBeTrue();
    [Fact] void should_drop_the_failed_build() => _oldIndexes.ShouldEqual(0);
    [Fact] void should_leave_no_invalid_index() => _invalidIndexes.ShouldEqual(0);
    [Fact] void should_rebuild_one_valid_index_idempotently() => _validIndexes.ShouldEqual(1);
}
