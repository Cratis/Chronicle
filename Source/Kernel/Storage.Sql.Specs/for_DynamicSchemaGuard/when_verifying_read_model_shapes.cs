// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.for_DynamicSchemaGuard;

public class when_verifying_read_model_shapes : EventStores.Namespaces.ReadModels.for_ReadModelMigrator.given.a_read_model_migrator
{
    Exception? _missing;
    Exception? _current;
    Exception? _changed;

    async Task Because()
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"}}}""");
        await using var context = CreateContext("verified_read_model", ProjectedColumns.ForSchema(schema));
        _missing = await Inspect(context);
        await context.EnsureTableExists();
        _current = await Inspect(context);
        var indexes = await context.Database.SqlQuery<string>($"SELECT name AS Value FROM pragma_index_list({context.TableName}) WHERE name LIKE 'chronicle_imx_%'").ToArrayAsync();
        var migration = new MigrationBuilder(context.Database.ProviderName);
        foreach (var index in indexes) migration.DropIndex(index, context.TableName);
        await new TableMigrator<ReadModelDbContext>(Substitute.For<ILogger<TableMigrator<ReadModelDbContext>>>()).ExecuteMigrationOperations(context, migration);
        await context.Database.ExecuteSqlRawAsync("ALTER TABLE verified_read_model RENAME COLUMN id TO old_id; ALTER TABLE verified_read_model ADD COLUMN id INTEGER");
        _changed = await Inspect(context);
    }

    static async Task<Exception?> Inspect(ReadModelDbContext context)
    {
        using var inspection = DynamicSchemaVerification.Begin();
        return await Catch.Exception(async () =>
        {
            await context.EnsureTableExists();
            DynamicSchemaVerification.Complete("test");
        });
    }

    [Fact] void should_reject_a_missing_table() => _missing.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] void should_accept_a_provisioned_table() => _current.ShouldBeNull();
    [Fact] void should_report_the_wrong_column_type() => ((SchemaNotCurrent)_changed!).Pending.Any(item => item.Contains("id", StringComparison.Ordinal) && item.Contains("INTEGER", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_also_report_the_missing_marker_index() => ((SchemaNotCurrent)_changed!).Pending.Any(item => item.Contains("initialized-marker", StringComparison.Ordinal)).ShouldBeTrue();
}
