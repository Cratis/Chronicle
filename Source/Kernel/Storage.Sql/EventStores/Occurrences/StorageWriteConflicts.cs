// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;

/// <summary>
/// Classifies provider write failures; callers settle unique collisions by durable lookup.
/// </summary>
internal static class StorageWriteConflicts
{
    /// <summary>
    /// Identifies a unique violation, without deciding which identity won.
    /// </summary>
    /// <param name="error">The provider error.</param>
    /// <returns>Whether a unique constraint refused the write.</returns>
    public static bool IsUnique(Exception error) => (error is DbUpdateException update ? update.InnerException : error) switch
    {
        PostgresException postgres => postgres.SqlState == PostgresErrorCodes.UniqueViolation,
        SqlException sql => sql.Number is 2601 or 2627,
        SqliteException sqlite => sqlite.SqliteExtendedErrorCode is 1555 or 2067,
        _ => false
    };

    /// <summary>
    /// Identifies a transaction known to have been refused rather than committed.
    /// </summary>
    /// <param name="error">The provider error.</param>
    /// <returns>Whether the write conflicted with another transaction.</returns>
    public static bool IsTransactionConflict(Exception error) => (error is DbUpdateException update ? update.InnerException : error) switch
    {
        PostgresException postgres => postgres.SqlState == PostgresErrorCodes.SerializationFailure || postgres.SqlState == PostgresErrorCodes.DeadlockDetected,
        SqlException sql => sql.Number == 1205,
        SqliteException sqlite => sqlite.SqliteErrorCode is 5 or 6,
        _ => false
    };
}
