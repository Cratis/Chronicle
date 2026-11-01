// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelMigrator.when_ensuring_table_is_migrated;

public class and_creating_the_initialized_marker_index : given.a_read_model_migrator
{
    const string TableName = "new_initialized_read_models";
    string[] _firstIndexes;
    string[] _secondIndexes;
    string[] _indexedColumns;
    long _unique;
    long _descending;

    static Task<string[]> MarkerIndexes(ReadModelDbContext context) => context.Database.SqlQuery<string>(
        $"SELECT name AS Value FROM sqlite_master WHERE type='index' AND tbl_name={TableName} AND substr(name,1,14)={InitializedMarkerIndex.Prefix}").ToArrayAsync();

    async Task Because()
    {
        var columns = ProjectedColumns.ForSchema(await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"string"},"status":{"type":"string"}}}
            """));
        await using var context = CreateContext(TableName, columns);
        await _migrator.EnsureTableMigrated(TableName, context);
        _firstIndexes = await MarkerIndexes(context);
        _migrator.ClearMigrationCache($"{context.Database.GetConnectionString()}:{TableName}");
        await _migrator.EnsureTableMigrated(TableName, context);
        _secondIndexes = await MarkerIndexes(context);
        var index = _firstIndexes.Single();
        _indexedColumns = await context.Database.SqlQuery<string>($"SELECT name AS Value FROM pragma_index_info({index})").ToArrayAsync();
        _unique = await context.Database.SqlQuery<long>($"SELECT \"unique\" AS Value FROM pragma_index_list({TableName}) WHERE name={index}").SingleAsync();
        _descending = await context.Database.SqlQuery<long>($"SELECT \"desc\" AS Value FROM pragma_index_xinfo({index}) WHERE key=1").SingleAsync();
    }

    [Fact] void should_create_one_marker_index() => _firstIndexes.Length.ShouldEqual(1);
    [Fact] void should_reuse_the_same_index_on_a_second_run() => _secondIndexes.ShouldEqual(_firstIndexes);
    [Fact] void should_index_only_the_initialized_marker() => _indexedColumns.Single().ShouldEqual(WellKnownProperties.ReadModelInstanceInitialized);
    [Fact] void should_be_non_unique() => _unique.ShouldEqual(0L);
    [Fact] void should_be_ascending() => _descending.ShouldEqual(0L);
}
