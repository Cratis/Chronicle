// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Classifies provider errors shared by the SQL storage.
/// </summary>
static class SqlErrors
{
    /// <summary>
    /// Determines whether the provider reported a relation that does not exist.
    /// </summary>
    /// <param name="error">The error to classify.</param>
    /// <returns>True when the relation is undefined; otherwise false.</returns>
    internal static bool IsMissingTable(Exception error) => error switch
    {
        PostgresException postgres => postgres.SqlState == PostgresErrorCodes.UndefinedTable,
        SqlException sql => sql.Number == 208,
        SqliteException sqlite => sqlite.SqliteErrorCode == 1 && sqlite.Message.Contains("no such table", StringComparison.Ordinal),
        _ => false
    };
}
