// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;
using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Probes all relations using parameterized exact text, never PostgreSQL's truncating name coercion.
/// </summary>
/// <param name="context">A metadata-only schema-purpose context at the resolved placement.</param>
/// <param name="schema">The resolved schema, not a logical identity.</param>
internal sealed class ReadModelTableCatalog(DbContext context, string schema) : IReadModelTableCatalog
{
    /// <inheritdoc/>
    public DatabaseType Provider => context.Database.GetDatabaseType();

    /// <inheritdoc/>
    public async Task<string?> Find(string name)
    {
        var sql = Provider switch
        {
            DatabaseType.PostgreSql => "SELECT c.relname::text FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace WHERE n.nspname::text = @schema::text AND c.relname::text COLLATE \"C\" = @name::text COLLATE \"C\"",
            DatabaseType.SqlServer => "SELECT o.name FROM sys.objects o JOIN sys.schemas s ON s.schema_id = o.schema_id WHERE s.name = @schema COLLATE DATABASE_DEFAULT AND o.name = @name COLLATE DATABASE_DEFAULT",
            DatabaseType.Sqlite => "SELECT name FROM sqlite_schema WHERE name = @name COLLATE NOCASE",
            _ => throw new NotSupportedException("Unsupported read model table catalog provider")
        };
        return await Scalar(sql, ("schema", schema), ("name", name)) as string;
    }

    /// <inheritdoc/>
    public async Task<bool> Equal(string left, string right)
    {
        if (Provider == DatabaseType.Sqlite)
        {
            return SqliteIdentifiersEqual(left, right);
        }
        if (Provider != DatabaseType.SqlServer)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }
        return Convert.ToInt32(await Scalar("SELECT CASE WHEN @left COLLATE DATABASE_DEFAULT = @right COLLATE DATABASE_DEFAULT THEN 1 ELSE 0 END", ("left", left), ("right", right)), System.Globalization.CultureInfo.InvariantCulture) == 1;
    }

    /// <summary>
    /// Compares SQLite identifiers with native NOCASE's ASCII-only folding, without allocating or opening a connection.
    /// </summary>
    /// <param name="left">The first identifier.</param>
    /// <param name="right">The second identifier.</param>
    /// <returns>Whether the names differ only in ASCII letter case.</returns>
    internal static bool SqliteIdentifiersEqual(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }
        for (var index = 0; index < left.Length; index++)
        {
            if (FoldAscii(left[index]) != FoldAscii(right[index]))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Executes a parameterized scalar metadata query.
    /// </summary>
    /// <param name="sql">The statement text.</param>
    /// <param name="parameters">The string parameters.</param>
    /// <returns>The scalar result.</returns>
    internal async Task<object?> Scalar(string sql, params (string Name, string Value)[] parameters)
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
#pragma warning disable CA2100 // SQL is provider-selected constant text; all catalog values are parameters.
            command.CommandText = sql;
#pragma warning restore CA2100
            command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
            foreach (var (name, value) in parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.DbType = DbType.String;
                parameter.Value = value;
                command.Parameters.Add(parameter);
            }
            return await command.ExecuteScalarAsync();
        }
        finally
        {
            if (close)
            {
                await context.Database.CloseConnectionAsync();
            }
        }
    }

    static char FoldAscii(char character) => character is >= 'A' and <= 'Z' ? (char)(character + ('a' - 'A')) : character;
}
