// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelMigrator.when_ensuring_table_is_migrated;

public class and_shadow_indexes_are_current : given.a_read_model_migrator
{
    const string TableName = "current_shadow_indexes";
    IReadOnlyList<ProjectedColumn> _columns;
    int _schemaLockCalls;
    string[] _originalIndexes;
    string[] _indexes;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"object","properties":{"part":{"type":"string"}}}}}""");
        _columns = ProjectedColumns.ForSchema(schema);
        await using var context = CreateContext(TableName, _columns);
        await _migrator.EnsureTableMigrated(TableName, context);
        _originalIndexes = await context.Database.SqlQuery<string>($"SELECT name AS Value FROM pragma_index_list('current_shadow_indexes') WHERE name LIKE 'chronicle_shx_%'").ToArrayAsync();
        _migrator.ClearMigrationCache(context.Database.GetConnectionString()!);
    }

    async Task Because()
    {
        await using var context = CreateContext(TableName, _columns);
        _schemaLockCalls = await EnsureTableMigratedAndCountSchemaLocks(context);
        _indexes = await context.Database.SqlQuery<string>($"SELECT name AS Value FROM pragma_index_list('current_shadow_indexes') WHERE name LIKE 'chronicle_shx_%'").ToArrayAsync();
    }

    [Fact] void should_not_enter_the_schema_lock() => _schemaLockCalls.ShouldEqual(0);
    [Fact] void should_have_an_existing_shadow_index() => _originalIndexes.Length.ShouldEqual(1);
    [Fact] void should_preserve_the_existing_shadow_index() => _indexes.ShouldEqual(_originalIndexes);
}
