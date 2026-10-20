// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelDbContext;

public class when_upgrading_sqlite_text_decimal_columns : Specification
{
    readonly string _path = Path.Combine(Path.GetTempPath(), $"legacy-decimals-{Guid.NewGuid():N}.db");
    SqliteConnection _connection;
    ReadModelDbContext _context;
    decimal[] _keys;
    decimal?[] _amounts;
    decimal?[] _range;
    decimal?[] _equal;
    decimal?[] _numericAmounts;
    string _storedType;
    string _storedValue;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"number","format":"decimal"},"amount":{"type":["number","null"],"format":"decimal"},"tax":{"type":["number","null"],"format":"decimal"}}}""");
        var columns = ProjectedColumns.ForSchema(schema);
        var migrator = new ReadModelMigrator(
            new TableMigrator<ReadModelDbContext>(Substitute.For<ILogger<TableMigrator<ReadModelDbContext>>>()),
            Substitute.For<ILogger<ReadModelMigrator>>());
        await using (var numeric = new SqliteConnection($"Data Source=numeric-{Guid.NewGuid():N};Mode=Memory;Cache=Shared"))
        {
            await numeric.OpenAsync();
            var numericOptions = new DbContextOptionsBuilder<ReadModelDbContext>().UseSqlite(numeric.ConnectionString).Options;
            await using var warm = new ReadModelDbContext(numericOptions, "legacy_decimals", columns, migrator);
            await warm.EnsureTableExists();
            _ = warm.Model;
        }
        _connection = new SqliteConnection($"Data Source={_path}");
        await _connection.OpenAsync();
        await using (var command = _connection.CreateCommand())
        {
            command.CommandText = "CREATE TABLE legacy_decimals (id TEXT PRIMARY KEY, AMOUNT TEXT NULL); INSERT INTO legacy_decimals (id, amount) VALUES ('2', '2.25')";
            await command.ExecuteNonQueryAsync();
        }
        var options = new DbContextOptionsBuilder<ReadModelDbContext>().UseSqlite(_connection.ConnectionString).Options;
        _context = new ReadModelDbContext(options, "legacy_decimals", columns, migrator);
        await _context.EnsureTableExists();
    }

    async Task Because()
    {
        var existing = (await _context.Entries.FindAsync(2m))!;
        existing["amount"] = 2.5000m;
        existing["tax"] = 2.25m;
        foreach (var value in new[] { 10m, 100m })
        {
            _context.Entries.Add(new DynamicReadModelEntity { ["id"] = value, ["amount"] = value + 0.25m, ["tax"] = value + 0.25m });
        }
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        _keys = await _context.Entries.OrderBy(entry => EF.Property<decimal>(entry, "id")).Select(entry => EF.Property<decimal>(entry, "id")).ToArrayAsync();
        _amounts = await _context.Entries.OrderBy(entry => EF.Property<decimal?>(entry, "amount")).Select(entry => EF.Property<decimal?>(entry, "amount")).ToArrayAsync();
        _range = await _context.Entries.Where(entry => EF.Property<decimal?>(entry, "amount") > 2m).Select(entry => EF.Property<decimal?>(entry, "amount")).ToArrayAsync();
        _equal = await _context.Entries.Where(entry => EF.Property<decimal?>(entry, "amount") == 10.25m).Select(entry => EF.Property<decimal?>(entry, "amount")).ToArrayAsync();
        _numericAmounts = await _context.Entries.OrderBy(entry => EF.Property<decimal?>(entry, "tax")).Select(entry => EF.Property<decimal?>(entry, "tax")).ToArrayAsync();
        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT typeof(amount), amount FROM legacy_decimals WHERE id = '2'";
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        _storedType = reader.GetString(0);
        _storedValue = reader.GetString(1);
    }

    async Task Destroy()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
        File.Delete(_path);
    }

    [Fact] void should_retain_existing_text_key_order() => _keys.ShouldEqual([10m, 100m, 2m]);
    [Fact] void should_read_old_and_new_text_values_in_legacy_order() => _amounts.ShouldEqual([10.25m, 100.25m, 2.5m]);
    [Fact] void should_preserve_legacy_text_range_comparisons() => _range.ShouldEqual([2.5m]);
    [Fact] void should_filter_text_decimal_equality_after_writing() => _equal.ShouldEqual([10.25m]);
    [Fact] void should_store_new_columns_numerically_in_the_same_table() => _numericAmounts.ShouldEqual([2.25m, 10.25m, 100.25m]);
    [Fact] void should_keep_the_physical_text_representation() => _storedType.ShouldEqual("text");
    [Fact] void should_preserve_invariant_decimal_text_and_scale() => _storedValue.ShouldEqual("2.5000");
}
