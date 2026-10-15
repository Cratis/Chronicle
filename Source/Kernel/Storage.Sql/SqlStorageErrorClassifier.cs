// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Classifies failures exposed by the supported SQL drivers.
/// </summary>
public class SqlStorageErrorClassifier : IStorageErrorClassifier
{
    /// <inheritdoc/>
    public bool IsTransient(Exception exception) =>
        exception is OperationCanceledException or TimeoutException or DbException { IsTransient: true } ||
        (exception is SqlException sql && sql.Errors.Cast<SqlError>().Any(error => IsTransientSqlServerNumber(error.Number))) ||
        exception is SqliteException { SqliteErrorCode: 5 or 6 } ||
        (exception is AggregateException aggregate
            ? aggregate.InnerExceptions.Any(IsTransient)
            : exception.InnerException is { } inner && IsTransient(inner));

    /// <summary>
    /// Classifies SqlError.Number without requiring the driver's non-public exception constructors.
    /// </summary>
    /// <param name="number">The SQL Server error number.</param>
    /// <returns>Whether it is a timeout (-2) or a deadlock victim (1205).</returns>
    internal static bool IsTransientSqlServerNumber(int number) => number is -2 or 1205;
}
