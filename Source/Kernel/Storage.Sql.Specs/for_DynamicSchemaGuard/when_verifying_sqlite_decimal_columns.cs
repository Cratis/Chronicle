// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_DynamicSchemaGuard;

public class when_verifying_sqlite_decimal_columns : EventStores.Namespaces.ReadModels.for_ReadModelMigrator.given.a_read_model_migrator
{
    Exception? _current;
    Exception? _changed;
    Exception? _wrongExpression;
    Exception? _untyped;
    string[] _columns;

    async Task Because()
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"number","format":"decimal"},"amount":{"type":"number","format":"decimal"}}}""");
        await using var context = CreateContext("verified_decimals", ProjectedColumns.ForSchema(schema));
        await context.EnsureTableExists();
        _columns = (await SchemaTables.Read(context))[context.TableName];
        _current = await Inspect(context);
        await context.Database.ExecuteSqlRawAsync("ALTER TABLE verified_decimals RENAME COLUMN amount TO old_amount; ALTER TABLE verified_decimals ADD COLUMN amount INTEGER");
        _changed = await Inspect(context);
        await context.Database.ExecuteSqlRawAsync("ALTER TABLE verified_decimals DROP COLUMN __order_amount; ALTER TABLE verified_decimals ADD COLUMN __order_amount AS ('not a decimal ordering key')");
        _wrongExpression = await Inspect(context);
        await context.Database.ExecuteSqlRawAsync("ALTER TABLE verified_decimals DROP COLUMN __order_amount; ALTER TABLE verified_decimals ADD COLUMN __order_amount");
        _untyped = await Inspect(context);
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

    [Fact] void should_accept_decimal_keys_and_properties_with_generated_ordering_columns() => _current.ShouldBeNull();
    [Fact] void should_include_the_generated_key_column_in_the_catalog() => _columns.ShouldContain(SqliteDecimalSortKey.ColumnNameFor("id"));
    [Fact] void should_include_the_generated_property_column_in_the_catalog() => _columns.ShouldContain(SqliteDecimalSortKey.ColumnNameFor("amount"));
    [Fact] void should_still_reject_a_wrong_decimal_storage_type() => ((SchemaNotCurrent)_changed!).Pending.Any(item => item.Contains("amount", StringComparison.Ordinal) && item.Contains("INTEGER", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reject_a_different_untyped_generated_expression() => ((SchemaNotCurrent)_wrongExpression!).Pending.Any(item => item.Contains("__order_amount", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_accept_an_untyped_ordinary_column() => ((SchemaNotCurrent)_untyped!).Pending.Any(item => item.Contains("__order_amount", StringComparison.Ordinal)).ShouldBeTrue();
}
