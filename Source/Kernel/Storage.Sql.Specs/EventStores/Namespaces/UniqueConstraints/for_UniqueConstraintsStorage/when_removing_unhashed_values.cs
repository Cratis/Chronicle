// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage;

public class when_removing_unhashed_values : Specification
{
    const string SourceTable = "log_unique_constraint";
    SqliteConnection _connection;
    UniqueConstraintDbContext _context;
    UniqueConstraintsStorage _storage;
    (long PerEventSource, long PerValue) _deleted;
    (long PerEventSource, long PerValue) _repeated;
    readonly string _hashShapedRawValue = new('a', 64);

    async Task Establish()
    {
        _connection = new($"DataSource=cleanup-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await _connection.OpenAsync();
        var migrator = new UniqueConstraintMigrator(
            new TableMigrator<UniqueConstraintDbContext>(Substitute.For<ILogger<TableMigrator<UniqueConstraintDbContext>>>()),
            new TableMigrator<UniqueConstraintValuesDbContext>(Substitute.For<ILogger<TableMigrator<UniqueConstraintValuesDbContext>>>()),
            Substitute.For<ILogger<UniqueConstraintMigrator>>());
        _context = new(new DbContextOptionsBuilder<UniqueConstraintDbContext>().UseSqlite(_connection.ConnectionString).WithoutServiceProviderCaching().Options, SourceTable, migrator);
        await _context.Database.ExecuteSqlRawAsync("CREATE TABLE log_unique_constraint (EventSourceId TEXT PRIMARY KEY, Value TEXT NOT NULL, SequenceNumber INTEGER NOT NULL); CREATE INDEX IX_log_unique_constraint_Value ON log_unique_constraint (Value); CREATE TABLE log_unique_constraint_values (Value TEXT PRIMARY KEY, EventSourceId TEXT NOT NULL, SequenceNumber INTEGER NOT NULL); CREATE TABLE other_unique_constraint (Value TEXT); INSERT INTO other_unique_constraint VALUES ('untouched')");
        var values = new[] { "personal-value", _hashShapedRawValue, new string('A', 64), new string('a', 63), new string('a', 65), _hashShapedRawValue + " ", _hashShapedRawValue + "\n", new string('g', 64) };
        for (var index = 0; index < values.Length; index++)
        {
            await _context.Database.ExecuteSqlRawAsync("INSERT INTO log_unique_constraint VALUES ({0}, {1}, 42)", index.ToString(), values[index]);
            await _context.Database.ExecuteSqlRawAsync("INSERT INTO log_unique_constraint_values VALUES ({0}, {1}, 42)", values[index], index.ToString());
        }
        await _context.Database.ExecuteSqlRawAsync("INSERT INTO log_unique_constraint VALUES ('duplicate', 'personal-value', 43)");

        // Another silo can have cached this duplicate fallback before the patch deletes the raw rows.
        await _context.EnsureTableExists();
        (await Scalar("SELECT \"unique\" FROM pragma_index_list('log_unique_constraint') WHERE name = 'IX_log_unique_constraint_Value'")).ShouldEqual(0L);
        var database = Substitute.For<IDatabase>();
        database.Namespace("store", "namespace").Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(
            new(new DbContextOptionsBuilder<NamespaceDbContext>().UseSqlite(_connection.ConnectionString).AddConceptAsSupport().Options), () => { })));
        _storage = new("store", "namespace", new EventSequenceId("log"), database);
    }

    async Task Because()
    {
        _deleted = await _storage.RemoveUnhashedValues();
        _repeated = await _storage.RemoveUnhashedValues();
    }

    [Fact] void should_count_deleted_source_claims() => _deleted.PerEventSource.ShouldEqual(8L);
    [Fact] void should_count_deleted_retained_claims() => _deleted.PerValue.ShouldEqual(7L);
    [Fact] void should_be_idempotent() => _repeated.ShouldEqual((0L, 0L));
    [Fact] async Task should_keep_a_hash_shaped_raw_value() => (await _context.Entries.Select(row => row.Value).SingleAsync()).ShouldEqual(_hashShapedRawValue);
    [Fact] async Task should_keep_only_the_hash_shaped_retained_value() => (await Scalar("SELECT COUNT(*) FROM log_unique_constraint_values")).ShouldEqual(1L);
    [Fact] async Task should_leave_another_sequence_alone() => (await Scalar("SELECT COUNT(*) FROM other_unique_constraint")).ShouldEqual(1L);
    [Fact] async Task should_replace_the_cached_fallback_with_a_unique_index() => (await Scalar("SELECT \"unique\" FROM pragma_index_list('log_unique_constraint') WHERE name = 'IX_log_unique_constraint_Value'")).ShouldEqual(1L);
    [Fact] async Task should_repair_the_retained_value_index() => (await Scalar("SELECT \"unique\" FROM pragma_index_list('log_unique_constraint_values') WHERE name = 'IX_log_unique_constraint_values_Value'")).ShouldEqual(1L);

    async Task<long> Scalar(string query)
    {
        await using var command = _connection.CreateCommand();
#pragma warning disable CA2100 // Only fixture-owned constant queries are executed.
        command.CommandText = query;
#pragma warning restore CA2100
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    async Task Destroy()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
