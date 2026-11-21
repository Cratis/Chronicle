// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;

#pragma warning disable EF1002 // Identifiers are provider-delimited; index filters come from SQL Server metadata, not callers.

/// <summary>
/// Keeps SQL Server claim comparisons consistent with the ordinal batch model.
/// </summary>
internal static class UniqueConstraintOrdinalColumns
{
    /// <summary>
    /// The binary collation shared by SQL Server claim columns and their EF models.
    /// </summary>
    internal const string SqlServerCollation = "Latin1_General_100_BIN2";

    /// <summary>
    /// Changes only claim identity collations, preserving column shapes and dependent index definitions atomically.
    /// </summary>
    /// <param name="context">The context connected to the namespace database.</param>
    /// <param name="tableName">The claim table.</param>
    /// <returns>The migration task.</returns>
    internal static async Task EnsureMigrated(DbContext context, string tableName)
    {
        if (!context.Database.IsSqlServer()) return;

        await using var transaction = context.Database.CurrentTransaction is null ? await context.Database.BeginTransactionAsync() : null;
        var table = new Table(context, tableName);
        await UniqueConstraintValueIndexMigration.LockTable(context, table.Identifier);
        var columns = await ReadColumns(table);
        if (columns.Count > 0) await MigrateColumns(table, columns);
        if (transaction is not null) await transaction.CommitAsync();
    }

    static async Task MigrateColumns(Table table, IReadOnlyCollection<Column> columns)
    {
        var indexes = await ReadIndexes(table);
        await ExecuteStatements(table, indexes.Select(index => index.Drop(table)));
        await ExecuteStatements(table, columns.Select(column => column.Alter(table)));
        await ExecuteStatements(table, indexes.Select(index => index.Create(table)));
    }

    static async Task ExecuteStatements(Table table, IEnumerable<string> statements)
    {
        foreach (var statement in statements) await table.Context.Database.ExecuteSqlRawAsync(statement);
    }

    static async Task<List<Column>> ReadColumns(Table table)
    {
        const string query = "SELECT name, TYPE_NAME(system_type_id), max_length, is_nullable, collation_name, " +
            "CONVERT(bit, CASE WHEN is_computed = 0 AND user_type_id = system_type_id " +
            "AND is_sparse = 0 AND is_column_set = 0 AND encryption_type IS NULL " +
            "AND generated_always_type = 0 AND is_hidden = 0 AND is_masked = 0 THEN 0 ELSE 1 END) " +
            "FROM sys.columns WHERE object_id = OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME(@table)) " +
            "AND name IN ('EventSourceId', 'Value')";
        await using var command = MetadataCommand(table, query);
        await using var reader = await command.ExecuteReaderAsync();
        var columns = new List<Column>();
        var count = 0;
        while (await reader.ReadAsync())
        {
            count++;
            var column = ReadColumn(reader, table);
            if (column is not null) columns.Add(column);
        }
        if (count != 2) throw Unsupported(table.Name);
        return columns;
    }

    static Column? ReadColumn(DbDataReader reader, Table table)
    {
        if (reader.IsDBNull(4)) throw Unsupported(table.Name);
        if (string.Equals(reader.GetString(4), SqlServerCollation, StringComparison.OrdinalIgnoreCase)) return null;
        if (reader.GetString(1) != "nvarchar" || reader.GetBoolean(5)) throw Unsupported(table.Name);
        var length = reader.GetInt16(2);
        return new(reader.GetString(0), length == -1 ? "MAX" : (length / 2).ToString(System.Globalization.CultureInfo.InvariantCulture), reader.GetBoolean(3));
    }

    static async Task<List<Index>> ReadIndexes(Table table)
    {
        const string query = "SELECT i.name, kc.type, i.is_unique, i.type, i.filter_definition, " +
            "c.name, ic.key_ordinal, ic.is_descending_key, ic.is_included_column, " +
            "CASE WHEN i.is_disabled = 0 AND i.is_hypothetical = 0 AND i.is_padded = 0 " +
            "AND i.fill_factor = 0 AND i.ignore_dup_key = 0 AND i.allow_row_locks = 1 " +
            "AND i.allow_page_locks = 1 AND i.data_space_id = FILEGROUP_ID('PRIMARY') " +
            "AND i.optimize_for_sequential_key = 0 AND i.suppress_dup_key_messages = 0 " +
            "AND NOT EXISTS (SELECT 1 FROM sys.stats s WHERE s.object_id = i.object_id " +
            "AND s.stats_id = i.index_id AND (s.no_recompute = 1 OR s.is_incremental = 1)) " +
            "AND NOT EXISTS (SELECT 1 FROM sys.partitions p WHERE p.object_id = i.object_id " +
            "AND p.index_id = i.index_id AND (p.data_compression <> 0 OR p.partition_number <> 1)) " +
            "THEN 1 ELSE 0 END AS supported FROM sys.indexes i " +
            "LEFT JOIN sys.key_constraints kc ON kc.parent_object_id = i.object_id AND kc.unique_index_id = i.index_id " +
            "JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id " +
            "JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id " +
            "WHERE i.object_id = OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME(@table)) " +
            "AND EXISTS (SELECT 1 FROM sys.index_columns dependency " +
            "JOIN sys.columns identity_column ON identity_column.object_id = dependency.object_id " +
            "AND identity_column.column_id = dependency.column_id " +
            "WHERE dependency.object_id = i.object_id AND dependency.index_id = i.index_id " +
            "AND identity_column.name IN ('EventSourceId', 'Value')) " +
            "ORDER BY i.index_id, ic.key_ordinal, ic.index_column_id";
        await using var command = MetadataCommand(table, query);
        await using var reader = await command.ExecuteReaderAsync();
        var indexes = new List<Index>();
        while (await reader.ReadAsync())
        {
            if (reader.GetByte(3) is not (1 or 2) || reader.GetInt32(9) != 1) throw Unsupported(table.Name);
            AppendIndex(indexes, reader, table.Sql);
        }
        return indexes;
    }

    static void AppendIndex(List<Index> indexes, DbDataReader reader, ISqlGenerationHelper sql)
    {
        var name = reader.GetString(0);
        if (indexes.Count == 0 || indexes[^1].Name != name)
        {
            indexes.Add(new(name, reader.IsDBNull(1) ? null : reader.GetString(1).Trim(), reader.GetBoolean(2), reader.GetByte(3) == 1, reader.IsDBNull(4) ? null : reader.GetString(4)));
        }
        var index = indexes[^1];
        var column = sql.DelimitIdentifier(reader.GetString(5));
        if (reader.GetBoolean(8)) index.Included.Add(column);
        else if (reader.GetByte(6) > 0) index.Keys.Add(column + (reader.GetBoolean(7) ? " DESC" : " ASC"));
        else throw Unsupported(name);
    }

    static DbCommand MetadataCommand(Table table, string query)
    {
        var command = table.Context.Database.GetDbConnection().CreateCommand();
#pragma warning disable CA2100 // Only fixed metadata queries supplied by this class; the table name is a parameter.
        command.CommandText = query;
#pragma warning restore CA2100
        command.Transaction = table.Context.Database.CurrentTransaction!.GetDbTransaction();
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@table";
        parameter.Value = table.Name;
        command.Parameters.Add(parameter);
        return command;
    }

    static InvalidOperationException Unsupported(string tableName) =>
        new($"Cannot migrate unique constraint table '{tableName}' to ordinal SQL Server collation: unsupported column or index dependency. Repair the schema while preserving claims before retrying.");

    sealed record Column(string Name, string Length, bool Nullable)
    {
        internal string Alter(Table table) =>
            $"ALTER TABLE {table.Identifier} ALTER COLUMN {table.Sql.DelimitIdentifier(Name)} NVARCHAR({Length}) COLLATE {SqlServerCollation} {(Nullable ? "NULL" : "NOT NULL")}";
    }

    sealed record Table(DbContext Context, string Name)
    {
        internal ISqlGenerationHelper Sql => Context.GetService<ISqlGenerationHelper>();
        internal string Identifier => Sql.DelimitIdentifier(Name);
    }

    sealed record Index(string Name, string? Constraint, bool Unique, bool Clustered, string? Filter)
    {
        internal List<string> Keys { get; } = [];
        internal List<string> Included { get; } = [];

        internal string Drop(Table table) => Constraint is null
            ? $"DROP INDEX {table.Sql.DelimitIdentifier(Name)} ON {table.Identifier}"
            : $"ALTER TABLE {table.Identifier} DROP CONSTRAINT {table.Sql.DelimitIdentifier(Name)}";

        internal string Create(Table table)
        {
            var kind = Clustered ? "CLUSTERED" : "NONCLUSTERED";
            var keys = string.Join(", ", Keys);
            if (Constraint is not null)
            {
                return $"ALTER TABLE {table.Identifier} ADD CONSTRAINT {table.Sql.DelimitIdentifier(Name)} {(Constraint == "PK" ? "PRIMARY KEY" : "UNIQUE")} {kind} ({keys})";
            }
            var included = Included.Count == 0 ? string.Empty : $" INCLUDE ({string.Join(", ", Included)})";
            var filter = Filter is null ? string.Empty : $" WHERE {Filter}";
            return $"CREATE {(Unique ? "UNIQUE " : string.Empty)}{kind} INDEX {table.Sql.DelimitIdentifier(Name)} ON {table.Identifier} ({keys}){included}{filter}";
        }
    }
}
