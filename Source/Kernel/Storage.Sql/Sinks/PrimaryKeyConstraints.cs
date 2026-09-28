// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Keeps the primary key constraint of a read model table named after the table, as <see cref="PrimaryKeyNames"/> names it.
/// </summary>
/// <remarks>
/// A replay builds a shadow table and renames it into place. Renaming a table does not rename its primary key,
/// and on PostgreSQL and SQL Server a constraint name is unique across the whole schema, so without this the live
/// table would keep holding <c language="csharp">PK_replay-{table}</c> and the next replay could not create its
/// shadow table. SQLite scopes constraint names to their table and needs nothing here.
/// </remarks>
internal static class PrimaryKeyConstraints
{
    /// <summary>
    /// Renames the primary key of a table to the name <see cref="PrimaryKeyNames"/> gives it, first moving that name off any
    /// other table still holding it from an earlier swap.
    /// </summary>
    /// <param name="scope">A scope whose connection reaches the database the table lives in.</param>
    /// <param name="table">The table whose primary key to name.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task NameAfterTable(DbContextScope<ReadModelDbContext> scope, string table)
    {
        var database = scope.DbContext.Database;
        var databaseType = database.GetDatabaseType();
        if (databaseType is not (DatabaseType.PostgreSql or DatabaseType.SqlServer))
        {
            return;
        }

        var current = await PrimaryKeyOf(database, databaseType, table);
        var expected = PrimaryKeyNames.For(databaseType, table);
        if (current is null || current == expected)
        {
            return;
        }

        var holder = await TableHolding(database, databaseType, expected);
        if (holder is not null && holder != PrimaryKeyNames.TableIdentifier(databaseType, table))
        {
            var holderCurrent = await PrimaryKeyOf(database, databaseType, holder);
            if (holderCurrent == expected)
            {
                await Rename(scope, databaseType, holder, expected, PrimaryKeyNames.For(databaseType, holder));
            }
        }

        await Rename(scope, databaseType, table, current, expected);
    }

    static async Task<string?> PrimaryKeyOf(DatabaseFacade database, DatabaseType databaseType, string table)
    {
        var name = PrimaryKeyNames.TableIdentifier(databaseType, table);
        var query = databaseType == DatabaseType.PostgreSql
            ? database.SqlQuery<string>($"SELECT k.conname AS \"Value\" FROM pg_constraint k JOIN pg_class c ON c.oid = k.conrelid JOIN pg_namespace n ON n.oid = c.relnamespace WHERE k.contype = 'p' AND c.relname = {name} AND n.nspname = current_schema()")
            : database.SqlQuery<string>($"SELECT name AS [Value] FROM sys.key_constraints WHERE type = 'PK' AND parent_object_id = OBJECT_ID(QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({name}))");

        return (await query.ToListAsync()).FirstOrDefault();
    }

    static async Task<string?> TableHolding(DatabaseFacade database, DatabaseType databaseType, string constraint)
    {
        var query = databaseType == DatabaseType.PostgreSql
            ? database.SqlQuery<string>($"SELECT c.relname AS \"Value\" FROM pg_constraint k JOIN pg_class c ON c.oid = k.conrelid JOIN pg_namespace n ON n.oid = c.relnamespace WHERE k.conname = {constraint} AND n.nspname = current_schema()")
            : database.SqlQuery<string>($"SELECT OBJECT_NAME(parent_object_id) AS [Value] FROM sys.key_constraints WHERE name = {constraint} AND schema_id = SCHEMA_ID()");

        return (await query.ToListAsync()).FirstOrDefault();
    }

    static async Task Rename(DbContextScope<ReadModelDbContext> scope, DatabaseType databaseType, string table, string from, string to)
    {
        var database = scope.DbContext.Database;
        if (databaseType == DatabaseType.SqlServer)
        {
            await database.ExecuteSqlAsync($"DECLARE @objname nvarchar(776) = QUOTENAME(SCHEMA_NAME()) + N'.' + QUOTENAME({from}); EXEC sp_rename @objname = @objname, @newname = {to}, @objtype = N'OBJECT';");
            return;
        }

        var sqlHelper = scope.DbContext.GetService<ISqlGenerationHelper>();
#pragma warning disable EF1002 // Every identifier is delimited by ISqlGenerationHelper.
        await database.ExecuteSqlRawAsync(
            $"ALTER TABLE {sqlHelper.DelimitIdentifier(table)} RENAME CONSTRAINT {sqlHelper.DelimitIdentifier(from)} TO {sqlHelper.DelimitIdentifier(to)}");
#pragma warning restore EF1002
    }
}
