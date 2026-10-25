// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_writing_a_composite_key;

public class and_an_old_kernel_promotes_a_table_without_the_shadow : given.a_composite_key_sink
{
    string _nameBeforeRepair;
    string _stamp;
    string _stampAfterOldWrite;
    string _oldRead;
    string[] _before;
    string[] _after;

    protected override ISqlSinkHarness CreateHarness() => new SqlSinkHarness();

    async Task Because()
    {
        await Write("a", "b", "initial");
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("DROP TABLE composite_shadows; CREATE TABLE composite_shadows(id TEXT NOT NULL PRIMARY KEY, name TEXT NULL, __lastHandledEventSequenceNumber INTEGER NULL, __initialized INTEGER NOT NULL DEFAULT 1); INSERT INTO composite_shadows(id,name) VALUES ('a_b','old kernel')");
        _nameBeforeRepair = (string)((IDictionary<string, object?>)(await _sink.GetInstances()).Instances.Single())["name"]!;
        await Write("a", "b", "new kernel");
        _stamp = await context.Database.SqlQuery<string>($"SELECT __compositeKey AS Value FROM composite_shadows WHERE id='a_b'").SingleAsync();
        _before = await context.Database.SqlQuery<string>($"SELECT name AS Value FROM pragma_index_list('composite_shadows') WHERE name LIKE 'chronicle_shx_%'").ToArrayAsync();
        var columns = new[]
        {
            new ProjectedColumn("id", typeof(string), true, false, false, false),
            new ProjectedColumn("name", typeof(string), false, false, false, true)
        };
        var options = new DbContextOptionsBuilder<ReadModelDbContext>().UseSqlite(_harness.ConnectionString).Options;
        await using var old = new ReadModelDbContext(options, Table, columns, Substitute.For<IReadModelMigrator>());
        await ReadModelIndexes.DropUndeclared(old, Table, []);
        await ReadModelIndexes.NameAfterTable(old, Table);
        var oldRow = await old.Entries.SingleAsync();
        _oldRead = (string)oldRow["id"]!;
        oldRow["name"] = "old kernel again";
        old.Entries.Entry(oldRow).Property("name").IsModified = true;
        await old.SaveChangesAsync();
        _stampAfterOldWrite = await context.Database.SqlQuery<string>($"SELECT __compositeKey AS Value FROM composite_shadows WHERE id='a_b'").SingleAsync();
        _after = await context.Database.SqlQuery<string>($"SELECT name AS Value FROM pragma_index_list('composite_shadows') WHERE name LIKE 'chronicle_shx_%'").ToArrayAsync();
    }

    [Fact] void should_allow_an_ordinary_read_before_shadow_repair() => _nameBeforeRepair.ShouldEqual("old kernel");
    [Fact] void should_stamp_after_repairing_the_missing_column() => _stamp.ShouldEqual("~k1:1:a1:b");
    [Fact] void should_restore_one_shadow_index() => _before.Length.ShouldEqual(1);
    [Fact] void should_leave_the_shadow_index_untouched_by_old_index_maintenance() => _after.ShouldEqual(_before);
    [Fact] void should_allow_an_old_kernel_to_read_the_same_row() => _oldRead.ShouldEqual("a_b");
    [Fact] void should_leave_the_stamp_untouched_by_an_old_kernel_write() => _stampAfterOldWrite.ShouldEqual(_stamp);
}
