// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelMigrator;

public class when_an_old_kernel_promotes_a_table_without_shadows : given.a_read_model_migrator
{
    int _attempts;
    decimal _ordinaryValue;
    string _key;
    string[] _indexesBefore;
    string[] _indexesAfter;
    string[] _mapped;
    long _shadowCount;

    async Task Because()
    {
        var columns = ProjectedColumns.ForSchema(await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"amount":{"type":"number","format":"decimal"}}}"""), [new IndexDefinition("amount")]);
        await using var context = CreateContext("promoted", columns);
        await context.EnsureTableExists();
        _mapped = context.Model.FindEntityType("promoted")!.GetProperties().Select(property => property.Name).ToArray();
        await context.Database.ExecuteSqlRawAsync("DROP TABLE promoted; CREATE TABLE promoted(id TEXT NOT NULL PRIMARY KEY, amount TEXT NULL, __lastHandledEventSequenceNumber INTEGER NULL, __initialized INTEGER NOT NULL DEFAULT 1); INSERT INTO promoted(id,amount) VALUES ('row','193.580')");
        await context.EnsureTableExists();
        _shadowCount = await context.Database.SqlQuery<long>($"SELECT count(*) AS Value FROM pragma_table_xinfo('promoted') WHERE hidden=2").SingleAsync();
        var row = await context.Entries.SingleAsync();
        row["amount"] = 2.00m;
        await context.SaveChangesAsync();
        _ordinaryValue = (decimal)row["amount"]!;
        _key = await ShadowColumns.Access(context, "__order_amount", async () =>
        {
            _attempts++;
            return await context.Database.SqlQuery<string>($"SELECT t.__order_amount AS Value FROM promoted t").SingleAsync();
        });
        _indexesBefore = await context.Database.SqlQuery<string>($"SELECT name AS Value FROM pragma_index_list('promoted') WHERE origin='c' AND substr(name, 1, 14) <> 'chronicle_imx_'").ToArrayAsync();
        await ReadModelIndexes.DropUndeclared(context, "promoted", []);
        await ReadModelIndexes.NameAfterTable(context, "promoted");
        await context.EnsureTableExists();
        _indexesAfter = await context.Database.SqlQuery<string>($"SELECT name AS Value FROM pragma_index_list('promoted') WHERE origin='c' AND substr(name, 1, 14) <> 'chronicle_imx_'").ToArrayAsync();
    }

    [Fact] void should_simulate_a_stale_migration_cache() => _shadowCount.ShouldEqual(0);
    [Fact] void should_leave_shadow_columns_out_of_ef() => _mapped.Any(name => name.StartsWith("__order_", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_allow_plain_reads_and_writes_before_repair() => _ordinaryValue.ShouldEqual(2m);
    [Fact] void should_retry_the_shadow_operation_once() => _attempts.ShouldEqual(2);
    [Fact] void should_restore_the_computed_key() => _key.ShouldEqual(SqliteDecimalSortKey.For(2m));
    [Fact] void should_restore_the_shadow_index() => _indexesBefore.Single().StartsWith(ShadowColumnIndexes.Prefix, StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_ignore_shadow_indexes_during_declared_index_maintenance() => _indexesAfter.ShouldEqual(_indexesBefore);
}
