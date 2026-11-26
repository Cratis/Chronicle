// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintMigrator.given;

#pragma warning disable CA2100 // DDL uses provider-delimited fixture identifiers; metadata queries parameterize table names.

public abstract class an_ordinal_claim_upgrade : Specification
{
    readonly List<(string[] Before, string[] After)> _preserved = [];
    readonly List<string[]> _collations = [];
    readonly List<Exception?> _failures = [];
    string _connection;
    string _serverCollation;
    string _databaseCollation;
    Func<Task> _drop;
    IUniqueConstraintMigrator _migrator;

    protected async Task Configure(string connection, Func<Task> drop)
    {
        // Altering the database collation invalidates pooled sessions ("Resetting the connection results in a
        // different state than the initial login"), so this fixture always opens fresh connections.
        _connection = new SqlConnectionStringBuilder(connection) { Pooling = false }.ConnectionString;
        _drop = drop;
        _serverCollation = (await Query("SELECT CONVERT(nvarchar(128), SERVERPROPERTY('Collation'))", string.Empty)).Single();
        var collation = string.Equals(_serverCollation, "Latin1_General_100_CI_AS", StringComparison.OrdinalIgnoreCase)
            ? "SQL_Latin1_General_CP1_CI_AS"
            : "Latin1_General_100_CI_AS";
        await Execute($"ALTER DATABASE CURRENT COLLATE {collation}");
        _databaseCollation = (await Query("SELECT CONVERT(nvarchar(128), DATABASEPROPERTYEX(DB_NAME(), 'Collation'))", string.Empty)).Single();
        _migrator = new UniqueConstraintMigrator(
            new TableMigrator<UniqueConstraintDbContext>(Substitute.For<ILogger<TableMigrator<UniqueConstraintDbContext>>>()),
            new TableMigrator<UniqueConstraintValuesDbContext>(Substitute.For<ILogger<TableMigrator<UniqueConstraintValuesDbContext>>>()),
            Substitute.For<ILogger<UniqueConstraintMigrator>>());
    }

    Task Destroy() => _drop();

    protected async Task CheckUpgrades()
    {
        foreach (var mode in new[] { UniqueConstraintMode.PerEventSource, UniqueConstraintMode.PerValue })
        {
            await CheckUpgrade(mode);
            await CheckUnsupported(mode);
            await CheckUnsupportedColumn(mode);
            await CheckNewTable(mode);
        }
    }

    protected void VerifyMetadataCollations() => _databaseCollation.ShouldNotEqual(_serverCollation);

    protected void VerifyPreservation()
    {
        foreach (var (before, after) in _preserved) after.ShouldEqual(before);
    }

    protected void VerifyCollations()
    {
        foreach (var collations in _collations) collations.ShouldEqual(["Latin1_General_100_BIN2", "Latin1_General_100_BIN2"]);
    }

    protected void VerifyFailures()
    {
        _failures.Count.ShouldEqual(6);
        foreach (var failure in _failures) failure.ShouldBeOfExactType<InvalidOperationException>();
    }

    async Task CheckUpgrade(UniqueConstraintMode mode)
    {
        var name = $"claims]\"_{mode}";
        await using var context = Context(mode, name);
        var table = Delimit(context, name);
        await CreateLegacy(context, table, name, mode);
        var before = await Snapshot(table);
        await Ensure(context);
        _preserved.Add((before, await Snapshot(table)));
        _collations.Add(await Collations(table));
        await Execute($"INSERT INTO {table} (EventSourceId, Value, SequenceNumber, Subject) VALUES (N'owner', N'CAFE', 8, N'case'), (N'Ｏwner', N'café', 9, N'accent')");
        var migrated = await Snapshot(table);
        await Ensure(context);
        await using var repeated = Context(mode, name);
        await Ensure(repeated);
        _preserved.Add((migrated, await Snapshot(table)));
    }

    async Task CheckUnsupported(UniqueConstraintMode mode)
    {
        var name = $"unsupported_{mode}";
        await using var context = Context(mode, name);
        var table = Delimit(context, name);
        await CreateLegacy(context, table, name, mode);
        await Execute($"CREATE INDEX [nondefault] ON {table} (EventSourceId) WITH (FILLFACTOR = 70)");
        var before = (await Snapshot(table)).Concat(await Collations(table)).ToArray();
        _failures.Add(await Catch.Exception(() => Ensure(context)));
        _failures.Add(await Catch.Exception(() => Ensure(context)));
        _preserved.Add((before, (await Snapshot(table)).Concat(await Collations(table)).ToArray()));
        await Execute($"DROP INDEX [nondefault] ON {table}");
        await Ensure(context);
        _collations.Add(await Collations(table));
    }

    async Task CheckUnsupportedColumn(UniqueConstraintMode mode)
    {
        var name = $"alias_{mode}";
        await using var context = Context(mode, name);
        var table = Delimit(context, name);
        await CreateLegacy(context, table, name, mode);
        var column = mode == UniqueConstraintMode.PerValue ? "EventSourceId" : "Value";
        var index = Delimit(context, $"IX_{name}_{column}");
        var type = Delimit(context, $"identity_{mode}");
        await Execute($"CREATE TYPE {type} FROM NVARCHAR(200) NOT NULL");
        await Execute($"DROP INDEX {index} ON {table}; DROP INDEX [extra] ON {table}; ALTER TABLE {table} ALTER COLUMN {column} {type} NOT NULL; CREATE INDEX {index} ON {table} ({column} DESC)");
        var before = (await Snapshot(table)).Concat(await Collations(table)).ToArray();
        _failures.Add(await Catch.Exception(() => Ensure(context)));
        _preserved.Add((before, (await Snapshot(table)).Concat(await Collations(table)).ToArray()));
    }

    async Task CheckNewTable(UniqueConstraintMode mode)
    {
        var name = $"new_{mode}";
        await using var context = Context(mode, name);
        await Ensure(context);
        _collations.Add(await Collations(Delimit(context, name)));
    }

    async Task CreateLegacy(DbContext context, string table, string name, UniqueConstraintMode mode)
    {
        var perValue = mode == UniqueConstraintMode.PerValue;
        var key = perValue ? "Value" : "EventSourceId";
        var indexed = perValue ? "EventSourceId" : "Value";
        var primary = Delimit(context, "PK_" + name);
        var index = Delimit(context, $"IX_{name}_{indexed}");
        var subject = Delimit(context, $"IX_{name}_Subject");
        await Execute($"""
            CREATE TABLE {table} (
                EventSourceId NVARCHAR(200) COLLATE Latin1_General_100_CI_AS NOT NULL,
                Value NVARCHAR({(perValue ? 200 : 400)}) COLLATE Latin1_General_100_CI_AS {(perValue ? "NOT NULL" : "NULL")},
                SequenceNumber DECIMAL(20,0) NOT NULL, Subject NVARCHAR(200) NULL DEFAULT N'default',
                CONSTRAINT {primary} PRIMARY KEY NONCLUSTERED ({key} DESC));
            CREATE {(perValue ? "" : "UNIQUE ")}INDEX {index} ON {table} ({indexed} DESC);
            CREATE INDEX {subject} ON {table} (Subject);
            CREATE INDEX [extra] ON {table} (EventSourceId DESC) INCLUDE (Subject) WHERE Subject IS NOT NULL;
            INSERT INTO {table} (EventSourceId, Value, SequenceNumber, Subject) VALUES (N'Owner', N'cafe', 7, N'original');
            """);
    }

    async Task<string[]> Snapshot(string table)
    {
        // Read catalog text separately and assemble snapshots in C#: concatenating it in SQL
        // mixes catalog, database-default, and persisted-definition collations.
        var columns = await Query("SELECT name, TYPE_NAME(user_type_id), max_length, is_nullable, OBJECT_DEFINITION(default_object_id) FROM sys.columns WHERE object_id = OBJECT_ID(@table) ORDER BY column_id", table);
        var indexes = await Query(
            """
            SELECT i.name, i.type, i.is_unique, kc.type, c.name, ic.key_ordinal, ic.is_descending_key, ic.is_included_column, i.filter_definition, i.fill_factor
            FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            LEFT JOIN sys.key_constraints kc ON kc.parent_object_id = i.object_id AND kc.unique_index_id = i.index_id
            WHERE i.object_id = OBJECT_ID(@table) ORDER BY i.name, ic.index_column_id
            """,
            table);
        var rows = await Query($"SELECT EventSourceId, Value, SequenceNumber, Subject FROM {table} ORDER BY EventSourceId COLLATE Latin1_General_100_BIN2", table);
        return [.. columns, .. indexes, .. rows];
    }

    Task<string[]> Collations(string table) => Query("SELECT collation_name FROM sys.columns WHERE object_id = OBJECT_ID(@table) AND name COLLATE DATABASE_DEFAULT IN (N'EventSourceId' COLLATE DATABASE_DEFAULT, N'Value' COLLATE DATABASE_DEFAULT) ORDER BY name", table);

    async Task<string[]> Query(string query, string table)
    {
        await using var connection = new SqlConnection(_connection);
        await connection.OpenAsync();
        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@table", table);
        await using var reader = await command.ExecuteReaderAsync();
        var result = new List<string>();
        while (await reader.ReadAsync())
        {
            result.Add(string.Join(':', Enumerable.Range(0, reader.FieldCount)
                .Select(index => reader.IsDBNull(index) ? string.Empty : Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture))));
        }
        return result.ToArray();
    }

    async Task Execute(string statement)
    {
        await using var connection = new SqlConnection(_connection);
        await connection.OpenAsync();
        await using var command = new SqlCommand(statement, connection);
        await command.ExecuteNonQueryAsync();
    }

    DbContext Context(UniqueConstraintMode mode, string table) => mode == UniqueConstraintMode.PerValue
        ? new UniqueConstraintValuesDbContext(new DbContextOptionsBuilder<UniqueConstraintValuesDbContext>().UseSqlServer(_connection).AddConceptAsSupport().WithoutServiceProviderCaching().Options, table, _migrator)
        : new UniqueConstraintDbContext(new DbContextOptionsBuilder<UniqueConstraintDbContext>().UseSqlServer(_connection).AddConceptAsSupport().WithoutServiceProviderCaching().Options, table, _migrator);

    static Task Ensure(DbContext context) => context is UniqueConstraintDbContext source ? source.EnsureTableExists() : ((UniqueConstraintValuesDbContext)context).EnsureTableExists();

    static string Delimit(DbContext context, string name) => context.GetService<ISqlGenerationHelper>().DelimitIdentifier(name);
}
