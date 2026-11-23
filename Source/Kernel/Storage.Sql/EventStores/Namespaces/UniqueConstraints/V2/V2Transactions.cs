// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.V2;

/// <summary>
/// Acquires SQLite's writer reservation before reading authority, avoiding deferred read-to-write upgrades.
/// Other providers serialize writers through the conditional authority UPDATE.
/// </summary>
static class V2Transactions
{
    /// <summary>
    /// Starts an operation-owned transaction with SQLite's immediate writer reservation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transaction owned by the caller.</returns>
    internal static async Task<IDbContextTransaction> Begin(V2DbContext context, CancellationToken cancellationToken)
    {
        await context.Database.OpenConnectionAsync(cancellationToken);
        if (context.Database.GetDbConnection() is not SqliteConnection sqlite)
        {
            return await context.Database.BeginTransactionAsync(cancellationToken);
        }

        // This is a new operation-owned connection, never the caller's shared mutable context.
        sqlite.DefaultTimeout = Math.Max(sqlite.DefaultTimeout, 30);
        cancellationToken.ThrowIfCancellationRequested();

        // Microsoft.Data.Sqlite uses BEGIN IMMEDIATE for Serializable (not deferred), and EF owns disposal.
        return await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
    }
}
