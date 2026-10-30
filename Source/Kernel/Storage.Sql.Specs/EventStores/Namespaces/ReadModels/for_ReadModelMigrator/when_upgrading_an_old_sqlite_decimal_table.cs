// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelMigrator;

public class when_upgrading_an_old_sqlite_decimal_table : Specification
{
    const string OldDdl = """
        CREATE TABLE "old_decimals" (
            "id" TEXT NOT NULL CONSTRAINT "PK_old_decimals" PRIMARY KEY,
            "amount" TEXT NULL,
            "__lastHandledEventSequenceNumber" INTEGER NULL,
            "__initialized" INTEGER NOT NULL DEFAULT 1
        );
        CREATE INDEX "chronicle_idx_old_decimals_amount_b1594d19fb4355fc" ON "old_decimals" ("amount");
        """;
    string _path;
    string _connectionString;
    ReadModelMigrator _migrator;
    string[] _numericKeys;
    string[] _before;
    string[] _after;
    long _shadows;
    long _indexes;
    string _addedKey;

    void Establish()
    {
        _path = Path.Combine(Path.GetTempPath(), $"chronicle_old_decimals_{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_path};Pooling=False";
        _migrator = Migrator();
    }

    void Destroy() => File.Delete(_path);

    ReadModelDbContext CreateContext(string table, IReadOnlyList<ProjectedColumn> columns) =>
        new(new DbContextOptionsBuilder<ReadModelDbContext>().UseSqlite(_connectionString).Options, table, columns, _migrator);

    static ReadModelMigrator Migrator() =>
        new(new TableMigrator<ReadModelDbContext>(Substitute.For<ILogger<TableMigrator<ReadModelDbContext>>>()), Substitute.For<ILogger<ReadModelMigrator>>());

    async Task Because()
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"number","format":"decimal"},"amount":{"type":"number","format":"decimal"},"added":{"type":"number","format":"decimal"}}}""");
        var columns = ProjectedColumns.ForSchema(schema);
        await using var context = CreateContext("old_decimals", columns);

        // Captured from the baseline migrator, before this change; not hand-invented REAL DDL.
        await context.Database.ExecuteSqlRawAsync(OldDdl);
        await context.Database.ExecuteSqlRawAsync("INSERT INTO old_decimals(id,amount) VALUES ('-10','193.580'), ('-2','193.58'), ('2','0.1234567890123456789012345678'), ('10','10.0'), ('9',NULL)");
        _before = await context.Database.SqlQuery<string>($"SELECT quote(id)||'|'||quote(amount) AS Value FROM old_decimals ORDER BY id").ToArrayAsync();
        await context.EnsureTableExists();
        await context.EnsureTableExists();

        // Simulate two process restarts against the same physical file and no migration cache.
        for (var restart = 0; restart < 2; restart++)
        {
            _migrator.ClearMigrationCache(_connectionString);
            _migrator = Migrator();
            await using var fresh = CreateContext("old_decimals", columns);
            await fresh.EnsureTableExists();
        }
        _numericKeys = await context.Database.SqlQuery<string>($"SELECT id AS Value FROM old_decimals ORDER BY __order_id,id").ToArrayAsync();
        _after = await context.Database.SqlQuery<string>($"SELECT quote(id)||'|'||quote(amount) AS Value FROM old_decimals ORDER BY id").ToArrayAsync();
        _shadows = await context.Database.SqlQuery<long>($"SELECT count(*) AS Value FROM pragma_table_xinfo('old_decimals') WHERE hidden=2").SingleAsync();
        _indexes = await context.Database.SqlQuery<long>($"SELECT count(*) AS Value FROM pragma_index_list('old_decimals') WHERE substr(name, 1, 14) <> 'chronicle_imx_'").SingleAsync();
        await context.Database.ExecuteSqlRawAsync("UPDATE old_decimals SET added='10.00' WHERE id='2'");
        _addedKey = await context.Database.SqlQuery<string>($"SELECT __order_added AS Value FROM old_decimals WHERE id='2'").SingleAsync();
    }

    [Fact] void should_sort_old_keys_numerically_after_two_restarts() => _numericKeys.ShouldEqual(["-10", "-2", "2", "9", "10"]);
    [Fact] void should_preserve_every_existing_text_byte() => _after.ShouldEqual(_before);
    [Fact] void should_add_one_virtual_column_per_decimal_including_the_new_property() => _shadows.ShouldEqual(3);
    [Fact] void should_not_rebuild_or_remove_existing_indexes() => _indexes.ShouldEqual(2);
    [Fact] void should_compute_the_sort_key_for_a_new_property() => _addedKey.ShouldEqual(SqliteDecimalSortKey.For(10m));
}
