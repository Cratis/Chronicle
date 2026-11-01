// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;
using System.Data.Common;
using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Refuses an incompatible pre-existing relation rather than adopting or upgrading user data.
/// </summary>
internal static class ReadModelTableBindingsSchema
{
    /// <summary>
    /// Validates the ledger shape and unique keys.
    /// </summary>
    /// <param name="context">The metadata-only ledger context.</param>
    /// <param name="schema">The resolved schema.</param>
    /// <returns>A task that completes when the shape is compatible.</returns>
    /// <exception cref="ReadModelTableBindingsSchemaMismatch">The relation is not the expected ledger.</exception>
    internal static async Task Validate(ReadModelTableBindingsDbContext context, string schema)
    {
        var columns = await Columns(context, schema);
        string[] text = ["Id", "EventStoreId", "Namespace", "ReadModelIdentifier", "ContainerName", "LogicalName", "PhysicalName", "ReplayAttempt"];
        string[] integers = ["Role", "State"];
        if (columns.Count != text.Length + integers.Length ||
            text.Any(name => !columns.Any(column => column.ColumnName == name && column.DataType == typeof(string))) ||
            integers.Any(name => !columns.Any(column => column.ColumnName == name && (column.DataType == typeof(int) || column.DataType == typeof(long)))) ||
            columns.Any(column => column.AllowDBNull != (column.ColumnName == "ReplayAttempt")))
        {
            throw new ReadModelTableBindingsSchemaMismatch();
        }
        if (!context.Database.IsSqlite() && columns.Any(column =>
            (column.ColumnName == "Id" && column.ColumnSize != 64) ||
            (column.ColumnName == "PhysicalName" && column.ColumnSize != 128)))
        {
            throw new ReadModelTableBindingsSchemaMismatch();
        }
        if (!await HasUniqueKey(context, schema, "Id") || !await HasUniqueKey(context, schema, "PhysicalName"))
        {
            throw new ReadModelTableBindingsSchemaMismatch();
        }
    }

    static async Task<IReadOnlyList<DbColumn>> Columns(ReadModelTableBindingsDbContext context, string schema)
    {
        var connection = context.Database.GetDbConnection();
        var close = connection.State != ConnectionState.Open;
        if (close)
        {
            await context.Database.OpenConnectionAsync();
        }
        try
        {
            await using var command = connection.CreateCommand();
            var helper = context.GetService<ISqlGenerationHelper>();
#pragma warning disable CA2100 // Both identifiers are delimited by the provider; no values enter the statement.
            command.CommandText = $"SELECT * FROM {helper.DelimitIdentifier(WellKnownTableNames.ReadModelTableNames, schema)} WHERE 1 = 0";
#pragma warning restore CA2100
            command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SchemaOnly);
            return await reader.GetColumnSchemaAsync();
        }
        finally
        {
            if (close)
            {
                await context.Database.CloseConnectionAsync();
            }
        }
    }

    static async Task<bool> HasUniqueKey(ReadModelTableBindingsDbContext context, string schema, string column)
    {
        var catalog = new ReadModelTableCatalog(context, schema);
        var sql = catalog.Provider switch
        {
            DatabaseType.PostgreSql => "SELECT count(*) FROM pg_catalog.pg_index i "
                + "JOIN pg_catalog.pg_class t ON t.oid = i.indrelid "
                + "JOIN pg_catalog.pg_namespace n ON n.oid = t.relnamespace "
                + "JOIN pg_catalog.pg_attribute a ON a.attrelid = t.oid AND a.attnum = i.indkey[0] "
                + "JOIN pg_catalog.pg_collation c ON c.oid = i.indcollation[0] "
                + "WHERE n.nspname::text = COALESCE(NULLIF(@schema::text, ''), current_schema()::text) AND t.relname::text = @table::text "
                + "AND a.attname::text = @column::text AND i.indisunique AND i.indisvalid "
                + "AND i.indnkeyatts = 1 AND i.indpred IS NULL AND c.collname::text = 'C'",
            DatabaseType.SqlServer => "SELECT count(*) FROM sys.indexes i "
                + "JOIN sys.objects t ON t.object_id = i.object_id "
                + "JOIN sys.schemas s ON s.schema_id = t.schema_id "
                + "JOIN sys.index_columns k ON k.object_id = i.object_id AND k.index_id = i.index_id AND k.key_ordinal = 1 "
                + "JOIN sys.columns c ON c.object_id = t.object_id AND c.column_id = k.column_id "
                + "WHERE s.name = COALESCE(NULLIF(@schema, N''), SCHEMA_NAME()) COLLATE DATABASE_DEFAULT AND t.name = @table COLLATE DATABASE_DEFAULT "
                + "AND c.name = @column COLLATE DATABASE_DEFAULT AND i.is_unique = 1 AND i.is_disabled = 0 AND i.has_filter = 0 "
                + "AND (SELECT count(*) FROM sys.index_columns x WHERE x.object_id = i.object_id AND x.index_id = i.index_id AND x.key_ordinal > 0) = 1 "
                + "AND c.collation_name = CASE WHEN @column = 'Id' THEN 'Latin1_General_100_BIN2' ELSE CONVERT(nvarchar(128), DATABASEPROPERTYEX(DB_NAME(), 'Collation')) END",
            DatabaseType.Sqlite => "SELECT count(*) FROM pragma_index_list('chronicle_read_model_table_names') i "
                + "WHERE i.\"unique\" = 1 AND i.partial = 0 "
                + "AND (SELECT count(*) FROM pragma_index_info(i.name)) = 1 "
                + "AND (SELECT name FROM pragma_index_info(i.name)) = @column COLLATE BINARY "
                + "AND (SELECT coll FROM pragma_index_xinfo(i.name) WHERE key = 1) = CASE WHEN @column = 'Id' THEN 'BINARY' ELSE 'NOCASE' END",
            _ => throw new NotSupportedException("Unsupported read model table ledger provider")
        };
        return Convert.ToInt64(await catalog.Scalar(sql, ("schema", schema), ("table", WellKnownTableNames.ReadModelTableNames), ("column", column)), System.Globalization.CultureInfo.InvariantCulture) > 0;
    }
}
