// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelMigrator.when_ensuring_table_is_migrated;

public class and_existing_rows_predate_the_initialization_column : given.a_read_model_migrator
{
    const string TableName = "existing_read_models";
    IReadOnlyList<ProjectedColumn> _columns;
    DynamicReadModelEntity _existing;
    DynamicReadModelEntity _placeholder;
    DynamicReadModelEntity _legacyInsert;

    async Task Establish()
    {
        _columns = ProjectedColumns.ForSchema(await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"string"},"status":{"type":"string"}}}
            """));
        var oldColumns = _columns.Where(column => column.Name != WellKnownProperties.ReadModelInstanceInitialized).ToArray();
        await using var context = CreateContext(TableName, oldColumns);
        await _migrator.EnsureTableMigrated(TableName, context);
        context.Entries.Add(new DynamicReadModelEntity { ["id"] = "existing", ["status"] = null });
        await context.SaveChangesAsync();
    }

    async Task Because()
    {
        await using var context = CreateContext(TableName, _columns);
        await _migrator.EnsureTableMigrated(TableName, context);
        await using var legacyContext = CreateContext(TableName, _columns.Where(column => column.Name != WellKnownProperties.ReadModelInstanceInitialized).ToArray());
        legacyContext.Entries.Add(new DynamicReadModelEntity { ["id"] = "legacy-writer", ["status"] = null });
        await legacyContext.SaveChangesAsync();
        context.Entries.Add(new DynamicReadModelEntity { ["id"] = "placeholder", [WellKnownProperties.ReadModelInstanceInitialized] = false });
        await context.SaveChangesAsync();
        var rows = await context.Entries.AsNoTracking().ToArrayAsync();
        _existing = rows.Single(row => (string)row["id"]! == "existing");
        _placeholder = rows.Single(row => (string)row["id"]! == "placeholder");
        _legacyInsert = rows.Single(row => (string)row["id"]! == "legacy-writer");
    }

    [Fact] void should_treat_existing_rows_as_initialized() => _existing[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(true);
    [Fact] void should_preserve_legitimate_null_values() => _existing["status"].ShouldBeNull();
    [Fact] void should_allow_new_placeholders_to_be_uninitialized() => _placeholder[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(false);
    [Fact] void should_initialize_rows_inserted_by_an_older_kernel_after_migration() => _legacyInsert[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(true);
    [Fact] void should_preserve_null_values_from_an_older_writer() => _legacyInsert["status"].ShouldBeNull();
}
