// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Extension methods for classifying a <see cref="DbUpdateException"/>.
/// </summary>
public static class DbUpdateExceptionExtensions
{
    const int SqliteConstraint = 19;
    const int SqliteConstraintPrimaryKey = 1555;
    const int SqliteConstraintUnique = 2067;
    const int SqlServerUniqueConstraint = 2627;
    const int SqlServerUniqueIndex = 2601;

    /// <summary>
    /// Check whether the write failed because a row with the same unique or primary key already exists.
    /// </summary>
    /// <param name="exception">The <see cref="DbUpdateException"/> to classify.</param>
    /// <returns>True if it is a unique or primary key violation on any supported provider; false otherwise.</returns>
    /// <remarks>
    /// Only this outcome means "someone else wrote it first". Every other write failure — a full disk, a lost
    /// connection, a violated check constraint — means nothing was written, and must not be mistaken for a lost race.
    /// </remarks>
    public static bool IsUniqueViolation(this DbUpdateException exception) => exception.InnerException switch
    {
        PostgresException postgres => postgres.SqlState == PostgresErrorCodes.UniqueViolation,
        SqliteException sqlite => sqlite.SqliteErrorCode == SqliteConstraint && sqlite.SqliteExtendedErrorCode is SqliteConstraintPrimaryKey or SqliteConstraintUnique,
        SqlException sqlServer => sqlServer.Number is SqlServerUniqueConstraint or SqlServerUniqueIndex,
        _ => false
    };
}
