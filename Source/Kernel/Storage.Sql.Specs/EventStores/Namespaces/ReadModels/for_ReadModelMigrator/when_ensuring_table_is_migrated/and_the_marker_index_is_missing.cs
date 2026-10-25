// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelMigrator.when_ensuring_table_is_migrated;

public class and_the_marker_index_is_missing : given.a_read_model_migrator
{
    const string TableName = "missing_marker_index";
    IReadOnlyList<ProjectedColumn> _columns;
    int _markerLockCalls;
    int _indexCount;

    Task<string[]> MarkerIndexes(ReadModelDbContext context) => context.Database.SqlQuery<string>(
        $"SELECT name AS Value FROM sqlite_master WHERE type='index' AND tbl_name={TableName} AND substr(name,1,14)={InitializedMarkerIndex.Prefix}").ToArrayAsync();

    async Task Establish()
    {
        _columns = ProjectedColumns.ForSchema(await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"status":{"type":"string"}}}"""));
        await using var context = CreateContext(TableName, _columns);
        await _migrator.EnsureTableMigrated(TableName, context);
        foreach (var index in await MarkerIndexes(context))
        {
            await ReadModelIndexOperations.Drop(context, TableName, index, online: true);
        }
        _migrator.ClearMigrationCache(context.Database.GetConnectionString()!);
    }

    async Task Because()
    {
        await using var context = CreateContext(TableName, _columns);
        _markerLockCalls = await EnsureTableMigratedAndCountMarkerLocks(context);
        _indexCount = (await MarkerIndexes(context)).Length;
    }

    [Fact] void should_enter_the_marker_index_lock_once() => _markerLockCalls.ShouldEqual(1);
    [Fact] void should_create_the_marker_index() => _indexCount.ShouldEqual(1);
}
