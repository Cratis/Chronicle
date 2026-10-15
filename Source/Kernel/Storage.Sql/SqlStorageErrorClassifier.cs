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
    /// <returns>Whether the provider reports a retryable connectivity, availability, throttling or deadlock failure.</returns>
    // SqlClient's default transient set (availability, throttling, connectivity and deadlock):
    // https://github.com/dotnet/SqlClient/blob/main/src/Microsoft.Data.SqlClient/src/Microsoft/Data/SqlClient/Reliability/SqlConfigurableRetryFactory.cs
    // 233, 10060, 4060, 40613, 40197, 49918-49920, 40501, 10928, 10929, 1205.
    // Additional transport failures (20, 64, 10053, 10054), without depending on EF Core's internal API:
    // https://github.com/dotnet/efcore/blob/v10.0.0/src/EFCore.SqlServer/Storage/Internal/SqlServerTransientExceptionDetector.cs
    // #4895 PR 3b review, finding 3: connection loss (53, -1) and our existing attempt timeout (-2).
    // A timed-out index write is safe to retry: it is idempotent and abandonment requires confirmation.
    internal static bool IsTransientSqlServerNumber(int number) => number is
        233 or 10060 or 4060 or 40613 or 40197 or 49918 or 49919 or 49920 or 40501 or 10928 or 10929 or 1205 or
        20 or 64 or 10053 or 10054 or 53 or -1 or -2;
}
