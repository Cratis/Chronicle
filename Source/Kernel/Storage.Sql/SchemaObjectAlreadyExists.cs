// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Recognizes the failure a schema change gets when another kernel created the same database object first.
/// </summary>
/// <remarks>
/// Kernels that do not take the database schema lock — for instance an older version during a rolling deploy — can
/// still race a creation. Their winner leaves the object in place, so the loser re-checks and continues.
/// </remarks>
internal static class SchemaObjectAlreadyExists
{
    static readonly FrozenSet<string> _duplicateStates = new[]
    {
        PostgresErrorCodes.DuplicateDatabase,
        PostgresErrorCodes.DuplicateSchema,
        PostgresErrorCodes.DuplicateTable,
        PostgresErrorCodes.DuplicateColumn,
        PostgresErrorCodes.DuplicateObject
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>
    /// Get whether a failure, or any failure it wraps, says the object already exists.
    /// </summary>
    /// <param name="exception">The failure.</param>
    /// <returns>True if the object already exists.</returns>
    internal static bool Is(Exception? exception)
    {
        while (exception is not null)
        {
            var exists = exception switch
            {
                SqlException sql => sql.Errors.Cast<SqlError>().Any(error => IsSqlServerNumber(error.Number)),
                PostgresException postgres => IsPostgreSqlState(postgres.SqlState, postgres.ConstraintName),
                AggregateException aggregate => aggregate.InnerExceptions.Any(Is),
                _ => false
            };

            // Other providers, SQLite among them, only say so in the message.
            if (exists || exception.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            exception = exception.InnerException;
        }

        return false;
    }

    /// <summary>
    /// Get whether a SQL Server error number says an object already exists.
    /// </summary>
    /// <param name="number">The SQL Server error number.</param>
    /// <returns>True if the object already exists.</returns>
    /// <remarks>
    /// 1801: database already exists. 2714: an object — table, constraint or schema — already exists.
    /// 1913: an index or statistics with the name already exists. 2705: a column name is specified more than once.
    /// </remarks>
    internal static bool IsSqlServerNumber(int number) => number is 1801 or 2714 or 1913 or 2705;

    /// <summary>
    /// Get whether a PostgreSQL state says an object already exists.
    /// </summary>
    /// <param name="sqlState">The SQLSTATE.</param>
    /// <param name="constraintName">The violated constraint, if any.</param>
    /// <returns>True if the object already exists.</returns>
    /// <remarks>
    /// 42P04 duplicate_database, 42P06 duplicate_schema, 42P07 duplicate_table (also relations such as indexes),
    /// 42701 duplicate_column and 42710 duplicate_object. Two concurrent <c language="sql">CREATE ... IF NOT EXISTS</c> can also fail
    /// with a unique violation on a system catalog index, whose names start with <c language="sql">pg_</c>; a unique violation on a
    /// table of ours is a real conflict and is not recognized.
    /// </remarks>
    internal static bool IsPostgreSqlState(string sqlState, string? constraintName) =>
        _duplicateStates.Contains(sqlState) ||
        (string.Equals(sqlState, PostgresErrorCodes.UniqueViolation, StringComparison.Ordinal) && constraintName?.StartsWith("pg_", StringComparison.Ordinal) == true);
}
