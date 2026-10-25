// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelMigrator.when_ensuring_table_is_migrated;

public class and_shadow_indexes_are_missing : given.a_read_model_migrator
{
    const string TableName = "missing_shadow_indexes";
    IReadOnlyList<ProjectedColumn> _columns;
    int _schemaLockCalls;
    int _indexCount;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"object","properties":{"part":{"type":"string"}}}}}""");
        _columns = ProjectedColumns.ForSchema(schema);
        await using var context = CreateContext(TableName, _columns.Select(column => column with { IsCompositeKey = false }).ToArray());
        await _migrator.EnsureTableMigrated(TableName, context);
        await context.Database.ExecuteSqlRawAsync("ALTER TABLE missing_shadow_indexes ADD COLUMN __compositeKey TEXT");
    }

    async Task Because()
    {
        await using var context = CreateContext(TableName, _columns);
        _schemaLockCalls = await EnsureTableMigratedAndCountSchemaLocks(context);
        _indexCount = await context.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM pragma_index_list('missing_shadow_indexes') WHERE name LIKE 'chronicle_shx_%'").SingleAsync();
    }

    [Fact] void should_enter_the_schema_lock_once() => _schemaLockCalls.ShouldEqual(1);
    [Fact] void should_create_the_shadow_index() => _indexCount.ShouldEqual(1);
}
