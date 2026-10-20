// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Recognizes the failure a database creation gets when a step after it blocks on another kernel's connection.
/// </summary>
/// <remarks>
/// SQL Server's own database creator issues <c language="sql">ALTER DATABASE ... SET READ_COMMITTED_SNAPSHOT ON</c>
/// right after <c language="sql">CREATE DATABASE</c>; that statement needs exclusive access to the database and
/// blocks while a sibling kernel already holds a connection to the one just created, which several kernels starting
/// at the same time against the same freshly created database do to each other. Unlike an "already exists" failure,
/// the provider reports this as an ordinary command timeout, so the database itself must be checked for directly
/// rather than inferred from the failure.
/// </remarks>
internal static class SchemaObjectCreationTimedOut
{
    /// <summary>
    /// Get whether a failure, or any failure it wraps, says a command timed out.
    /// </summary>
    /// <param name="exception">The failure.</param>
    /// <returns>True if a command timed out.</returns>
    internal static bool Is(Exception? exception)
    {
        while (exception is not null)
        {
            var timedOut = exception switch
            {
                SqlException sql => IsSqlServerNumber(sql.Number),
                PostgresException postgres => IsPostgreSqlState(postgres.SqlState),
                AggregateException aggregate => aggregate.InnerExceptions.Any(Is),
                _ => false
            };

            if (timedOut)
            {
                return true;
            }

            exception = exception.InnerException;
        }

        return false;
    }

    /// <summary>
    /// Get whether a SQL Server error number says a command timed out.
    /// </summary>
    /// <param name="number">The SQL Server error number.</param>
    /// <returns>True if a command timed out.</returns>
    /// <remarks>
    /// -2: the client-side command timeout elapsed, which is how SqlClient reports the provider never answering
    /// in time - including a server-side lock wait that outlasts it.
    /// </remarks>
    internal static bool IsSqlServerNumber(int number) => number == -2;

    /// <summary>
    /// Get whether a PostgreSQL state says a command timed out.
    /// </summary>
    /// <param name="sqlState">The SQLSTATE.</param>
    /// <returns>True if a command timed out.</returns>
    /// <remarks>
    /// 57014 query_canceled covers a statement_timeout cancellation; 55P03 lock_not_available covers a
    /// lock acquired with NOWAIT or a bounded lock_timeout.
    /// </remarks>
    internal static bool IsPostgreSqlState(string sqlState) =>
        string.Equals(sqlState, PostgresErrorCodes.QueryCanceled, StringComparison.Ordinal) ||
        string.Equals(sqlState, PostgresErrorCodes.LockNotAvailable, StringComparison.Ordinal);
}
