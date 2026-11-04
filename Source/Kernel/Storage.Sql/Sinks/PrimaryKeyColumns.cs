// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Reads which columns a read model table has and which of them form its primary key, spelled as the catalog spells them.
/// </summary>
internal static class PrimaryKeyColumns
{
    /// <summary>
    /// Reads the <see cref="TableLayout"/> of a table.
    /// </summary>
    /// <param name="database">The <see cref="DatabaseFacade"/> reaching the database the table lives in.</param>
    /// <param name="databaseType">The <see cref="DatabaseType"/> of the database.</param>
    /// <param name="table">The logical name of the table.</param>
    /// <returns>The <see cref="TableLayout"/>.</returns>
    public static async Task<TableLayout> LayoutOf(DatabaseFacade database, DatabaseType databaseType, string table) =>
        new(await ColumnsOf(database, databaseType, table), await NamesOf(database, databaseType, table));

    /// <summary>
    /// Gets the names of the primary key columns of a table, in no particular order.
    /// </summary>
    /// <param name="database">The <see cref="DatabaseFacade"/> reaching the database the table lives in.</param>
    /// <param name="databaseType">The <see cref="DatabaseType"/> of the database.</param>
    /// <param name="table">The logical name of the table.</param>
    /// <returns>The column names, or an empty list when the table does not exist or has no primary key.</returns>
    public static async Task<IReadOnlyList<string>> ColumnsOf(DatabaseFacade database, DatabaseType databaseType, string table)
    {
        var name = PrimaryKeyNames.TableIdentifier(databaseType, table);
        var query = databaseType switch
        {
            DatabaseType.PostgreSql => database.SqlQuery<string>($"SELECT a.attname::text AS \"Value\" FROM pg_index i JOIN pg_class c ON c.oid = i.indrelid JOIN pg_namespace n ON n.oid = c.relnamespace CROSS JOIN LATERAL unnest(i.indkey::int2[]) AS k(attnum) JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum = k.attnum WHERE i.indisprimary AND c.relname = {name} AND n.nspname = current_schema()"),
            DatabaseType.SqlServer => database.SqlQuery<string>($"SELECT c.name AS [Value] FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.is_primary_key = 1 AND i.object_id = OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({name}))"),
            _ => database.SqlQuery<string>($"SELECT name AS \"Value\" FROM pragma_table_info({name}) WHERE pk > 0")
        };

        return await query.ToListAsync();
    }

    /// <summary>
    /// Gets the names of all columns of a table.
    /// </summary>
    /// <param name="database">The <see cref="DatabaseFacade"/> reaching the database the table lives in.</param>
    /// <param name="databaseType">The <see cref="DatabaseType"/> of the database.</param>
    /// <param name="table">The logical name of the table.</param>
    /// <returns>The column names, or an empty list when the table does not exist.</returns>
    public static async Task<IReadOnlyList<string>> NamesOf(DatabaseFacade database, DatabaseType databaseType, string table)
    {
        var name = PrimaryKeyNames.TableIdentifier(databaseType, table);
        var query = databaseType switch
        {
            DatabaseType.PostgreSql => database.SqlQuery<string>($"SELECT a.attname::text AS \"Value\" FROM pg_attribute a JOIN pg_class c ON c.oid = a.attrelid JOIN pg_namespace n ON n.oid = c.relnamespace WHERE a.attnum > 0 AND NOT a.attisdropped AND c.relname = {name} AND n.nspname = current_schema()"),
            DatabaseType.SqlServer => database.SqlQuery<string>($"SELECT c.name AS [Value] FROM sys.columns c WHERE c.object_id = OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({name}))"),
            _ => database.SqlQuery<string>($"SELECT name AS \"Value\" FROM pragma_table_info({name})")
        };

        return await query.ToListAsync();
    }
}
