// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelMigrator.when_ensuring_table_is_migrated;

public class and_adding_the_initialized_marker_index_to_an_existing_table : given.a_read_model_migrator
{
    const string TableName = "old_initialized_read_models";
    IReadOnlyList<ProjectedColumn> _columns;
    string[] _beforeIndexes;
    string[] _firstIndexes;
    string[] _secondIndexes;

    static Task<string[]> MarkerIndexes(ReadModelDbContext context) => context.Database.SqlQuery<string>(
        $"SELECT name AS Value FROM sqlite_master WHERE type='index' AND tbl_name={TableName} AND substr(name,1,14)={InitializedMarkerIndex.Prefix}").ToArrayAsync();

    async Task Establish()
    {
        _columns = ProjectedColumns.ForSchema(await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"string"},"status":{"type":"string"}}}
            """));
        await using var context = CreateContext(TableName, _columns);
        await _migrator.EnsureTableMigrated(TableName, context);
        var index = (await MarkerIndexes(context)).Single();
        await ReadModelIndexOperations.Drop(context, TableName, index, online: true);
        _beforeIndexes = await MarkerIndexes(context);
        _migrator.ClearMigrationCache($"{context.Database.GetConnectionString()}:{TableName}");
    }

    async Task Because()
    {
        await using var context = CreateContext(TableName, _columns);
        await _migrator.EnsureTableMigrated(TableName, context);
        _firstIndexes = await MarkerIndexes(context);
        _migrator.ClearMigrationCache($"{context.Database.GetConnectionString()}:{TableName}");
        await _migrator.EnsureTableMigrated(TableName, context);
        _secondIndexes = await MarkerIndexes(context);
    }

    [Fact] void should_start_without_a_marker_index() => _beforeIndexes.ShouldBeEmpty();
    [Fact] void should_add_one_marker_index() => _firstIndexes.Length.ShouldEqual(1);
    [Fact] void should_not_duplicate_the_index_on_a_second_run() => _secondIndexes.ShouldEqual(_firstIndexes);
}
