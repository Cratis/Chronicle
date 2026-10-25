// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelMigrator.when_ensuring_table_is_migrated;

public class and_preserving_the_marker_index_during_reconciliation : given.a_read_model_migrator
{
    IReadOnlyList<ProjectedColumn> _columns;
    string[] _beforeIndexes;
    string[] _afterIndexes;

    static Task<string[]> MarkerIndexes(ReadModelDbContext context) => context.Database.SqlQuery<string>(
        $"SELECT name AS Value FROM sqlite_master WHERE type='index' AND tbl_name={context.TableName} AND substr(name,1,14)={InitializedMarkerIndex.Prefix}").ToArrayAsync();

    async Task Establish()
    {
        _columns = ProjectedColumns.ForSchema(await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"string"}}}
            """));
        await using var context = CreateContext("marker_before_rename", _columns);
        await _migrator.EnsureTableMigrated(context.TableName, context);
        _beforeIndexes = await MarkerIndexes(context);
    }

    async Task Because()
    {
        await using var original = CreateContext("marker_before_rename", _columns);
        await ReadModelIndexes.DropUndeclared(original, original.TableName, []);
        await original.Database.ExecuteSqlRawAsync("ALTER TABLE marker_before_rename RENAME TO marker_after_rename");
        await using var renamed = CreateContext("marker_after_rename", _columns);
        await ReadModelIndexes.NameAfterTable(renamed, renamed.TableName);
        await _migrator.EnsureTableMigrated(renamed.TableName, renamed);
        await ReadModelIndexes.DropUndeclared(renamed, renamed.TableName, []);
        _afterIndexes = await MarkerIndexes(renamed);
    }

    [Fact] void should_start_with_one_marker_index() => _beforeIndexes.Length.ShouldEqual(1);
    [Fact] void should_keep_the_marker_index_after_declared_indexes_are_reconciled_and_the_table_is_renamed() => _afterIndexes.ShouldEqual(_beforeIndexes);
}
