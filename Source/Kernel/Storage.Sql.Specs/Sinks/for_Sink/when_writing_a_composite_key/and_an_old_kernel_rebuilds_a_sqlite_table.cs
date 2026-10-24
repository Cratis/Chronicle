// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_writing_a_composite_key;

public class and_an_old_kernel_rebuilds_a_sqlite_table : given.a_composite_key_sink
{
    int _columnsAfterRebuild;
    int _indexesAfterRebuild;
    int _indexesAfterRepair;
    string _oldId;
    string _oldWrite;
    string _stamp;

    protected override ISqlSinkHarness CreateHarness() => new SqlSinkHarness();

    async Task Because()
    {
        await Write("a", "b", "new kernel");
        var columns = ProjectedColumns.ForSchema(CreateSchema()).Select(column => column with
        {
            IsCompositeKey = false,
            IsNullable = column.Name != "name" && column.IsNullable
        }).ToArray();
        var options = new DbContextOptionsBuilder<ReadModelDbContext>().UseSqlite(_harness.ConnectionString).Options;
        await using var old = new ReadModelDbContext(options, Table, columns, Substitute.For<IReadModelMigrator>());
        var migration = new MigrationBuilder(old.Database.ProviderName);

        // The old kernel's EF model does not contain the shadow. Exercise SQLite's actual ALTER emulation,
        // rather than manually dropping the unknown column or replacing the table in the spec.
        migration.AlterColumn<string>("name", Table, type: "TEXT", nullable: false, oldClrType: typeof(string), oldType: "TEXT", oldNullable: true);
        foreach (var command in old.GetService<IMigrationsSqlGenerator>().Generate(migration.Operations, old.GetService<IDesignTimeModel>().Model))
        {
            await command.ExecuteNonQueryAsync(old.GetService<IRelationalConnection>());
        }
        _columnsAfterRebuild = await old.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM pragma_table_xinfo('composite_shadows') WHERE name='__compositeKey'").SingleAsync();
        _indexesAfterRebuild = await old.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM pragma_index_list('composite_shadows') WHERE name LIKE 'chronicle_shx_%'").SingleAsync();
        var row = await old.Entries.SingleAsync();
        _oldId = (string)row["id"]!;
        row["name"] = "old kernel";
        old.Entries.Entry(row).Property("name").IsModified = true;
        await old.SaveChangesAsync();
        _oldWrite = (string)((IDictionary<string, object?>)(await _sink.GetInstances()).Instances.Single())["name"]!;
        await Write("a", "b", "repaired");
        _stamp = await old.Database.SqlQuery<string>($"SELECT __compositeKey AS Value FROM composite_shadows WHERE id='a_b'").SingleAsync();
        _indexesAfterRepair = await old.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM pragma_index_list('composite_shadows') WHERE name LIKE 'chronicle_shx_%'").SingleAsync();
    }

    [Fact] void should_allow_a_legacy_ef_rebuild_to_discard_the_unknown_column() => _columnsAfterRebuild.ShouldEqual(0);
    [Fact] void should_allow_the_rebuild_to_discard_the_unknown_index() => _indexesAfterRebuild.ShouldEqual(0);
    [Fact] void should_preserve_the_legacy_id_for_old_kernel_reads() => _oldId.ShouldEqual("a_b");
    [Fact] void should_allow_old_kernel_writes_before_repair() => _oldWrite.ShouldEqual("old kernel");
    [Fact] void should_restore_and_stamp_the_shadow_on_the_next_full_key_write() => _stamp.ShouldEqual("~k1:1:a1:b");
    [Fact] void should_restore_the_shadow_index() => _indexesAfterRepair.ShouldEqual(1);
}
