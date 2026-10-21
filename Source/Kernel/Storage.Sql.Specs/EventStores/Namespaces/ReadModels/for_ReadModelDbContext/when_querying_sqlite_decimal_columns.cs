// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelDbContext;

public class when_querying_sqlite_decimal_columns : Specification
{
    SqliteConnection _connection;
    ReadModelDbContext _context;
    decimal[] _orderedKeys;
    decimal?[] _orderedAmounts;
    decimal?[] _range;
    decimal?[] _equal;
    int _nullCount;

    async Task Establish()
    {
        _connection = new SqliteConnection($"Data Source=decimal-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await _connection.OpenAsync();
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"number","format":"decimal"},"amount":{"type":["number","null"],"format":"decimal"}}}""");
        var options = new DbContextOptionsBuilder<ReadModelDbContext>().UseSqlite(_connection.ConnectionString).Options;
        var migrator = new ReadModelMigrator(
            new TableMigrator<ReadModelDbContext>(Substitute.For<ILogger<TableMigrator<ReadModelDbContext>>>()),
            Substitute.For<ILogger<ReadModelMigrator>>());
        _context = new ReadModelDbContext(options, "decimal_queries", ProjectedColumns.ForSchema(schema), migrator);
        await _context.EnsureTableExists();
        foreach (var value in new[] { decimal.MaxValue, decimal.MinValue, 100m, -2m, 10m, -10m, 2m })
        {
            var entry = new DynamicReadModelEntity { ["id"] = value, ["amount"] = value };
            _context.Entries.Add(entry);
        }
        _context.Entries.Add(new DynamicReadModelEntity { ["id"] = 999m, ["amount"] = null });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    async Task Because()
    {
        _orderedKeys = await _context.Entries.OrderBy(entry => EF.Property<decimal>(entry, "id")).Select(entry => EF.Property<decimal>(entry, "id")).ToArrayAsync();
        _orderedAmounts = await _context.Entries.OrderBy(entry => EF.Property<decimal?>(entry, "amount")).Select(entry => EF.Property<decimal?>(entry, "amount")).ToArrayAsync();
        _range = await _context.Entries.Where(entry => EF.Property<decimal?>(entry, "amount") > 2m).OrderBy(entry => EF.Property<decimal?>(entry, "amount")).Select(entry => EF.Property<decimal?>(entry, "amount")).ToArrayAsync();
        _equal = await _context.Entries.Where(entry => EF.Property<decimal?>(entry, "amount") == 10m).Select(entry => EF.Property<decimal?>(entry, "amount")).ToArrayAsync();
        _nullCount = await _context.Entries.CountAsync(entry => EF.Property<decimal?>(entry, "amount") == null);
    }

    async Task Destroy()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact] void should_order_decimal_keys_numerically() => _orderedKeys.ShouldEqual([decimal.MinValue, -10m, -2m, 2m, 10m, 100m, 999m, decimal.MaxValue]);
    [Fact] void should_order_nullable_decimal_values_numerically() => _orderedAmounts.ShouldEqual([null, decimal.MinValue, -10m, -2m, 2m, 10m, 100m, decimal.MaxValue]);
    [Fact] void should_filter_ranges_numerically() => _range.ShouldEqual([10m, 100m, decimal.MaxValue]);
    [Fact] void should_filter_equal_values() => _equal.ShouldEqual([10m]);
    [Fact] void should_preserve_null_values() => _nullCount.ShouldEqual(1);
}
