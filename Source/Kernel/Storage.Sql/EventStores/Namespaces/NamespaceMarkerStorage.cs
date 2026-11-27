// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;

/// <summary>
/// Maintains the namespace-local singleton; conditional updates hold close/append conflicts through commit.
/// </summary>
/// <param name="open">Opens the namespace context, never the event-store control context.</param>
public sealed class NamespaceMarkerStorage(Func<Task<DbContextScope<NamespaceDbContext>>> open) : INamespaceMarkerStorage
{
    /// <inheritdoc/>
    public async Task<NamespaceMarker?> Get()
    {
        await using var scope = await open();
        var row = await scope.DbContext.Set<NamespaceMarkerRow>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == 1);
        return row is null ? null : new(new(row.Incarnation), row.IsOpen, row.Touch);
    }

    /// <inheritdoc/>
    public async Task<bool> TryOpen(NamespaceIncarnationId incarnation, NamespaceIncarnationId? previous)
    {
        if (incarnation.Value == Guid.Empty || previous?.Value == Guid.Empty) return false;
        var current = await Get();
        if (current is null) return await Insert(incarnation);
        if (current.Incarnation == incarnation && current.IsOpen) return true;
        if (current.IsOpen || previous != current.Incarnation) return false;
        await using var scope = await open();
        return await scope.DbContext.Set<NamespaceMarkerRow>()
            .Where(row => row.Id == 1 && row.Incarnation == current.Incarnation.Value && !row.IsOpen && row.Touch == current.Touch)
            .ExecuteUpdateAsync(set => set.SetProperty(row => row.Incarnation, incarnation.Value)
                .SetProperty(row => row.IsOpen, true).SetProperty(row => row.Touch, row => row.Touch + 1)) == 1;
    }

    /// <inheritdoc/>
    public async Task<bool> TryClose(NamespaceIncarnationId incarnation)
    {
        if (incarnation.Value == Guid.Empty) return false;
        await using var scope = await open();
        return await scope.DbContext.Set<NamespaceMarkerRow>()
            .Where(row => row.Id == 1 && row.Incarnation == incarnation.Value)
            .ExecuteUpdateAsync(set => set.SetProperty(row => row.IsOpen, false).SetProperty(row => row.Touch, row => row.Touch + 1)) == 1;
    }

    /// <summary>
    /// Writes the exact open marker in the caller's event/receipt transaction. Never creates a marker.
    /// The write lock serializes admitted transactions as well as close; this deliberately adds contention.
    /// </summary>
    /// <param name="database">The context already owning the protected event/receipt transaction.</param>
    /// <param name="expected">The exact expected lifetime.</param>
    /// <param name="cancellationToken">The activation cancellation.</param>
    /// <returns>Whether exactly one matching open marker was changed.</returns>
    /// <exception cref="InvalidOperationException">No transaction is active on this context.</exception>
    internal static async Task<bool> TryFence(DbContext database, NamespaceIncarnationId expected, CancellationToken cancellationToken) =>
        await GetFenceRejection(database, expected, cancellationToken) is null;

    /// <summary>Attempts the fence and diagnoses refusal in the same transaction, without re-admitting it.</summary>
    /// <param name="database">The protected transaction context.</param>
    /// <param name="expected">The supplied identity.</param>
    /// <param name="cancellationToken">The activation cancellation.</param>
    /// <returns>Null on admission; otherwise the typed refusal.</returns>
    /// <exception cref="InvalidOperationException">No transaction is active.</exception>
    internal static async Task<NamespaceCommitRejected?> GetFenceRejection(DbContext database, NamespaceIncarnationId expected, CancellationToken cancellationToken)
    {
        if (expected.Value == Guid.Empty) return new();
        if (database.Database.CurrentTransaction is null) throw new InvalidOperationException("A namespace fence requires an active event transaction.");
        try
        {
            var admitted = await database.Set<NamespaceMarkerRow>()
                .Where(row => row.Id == 1 && row.Incarnation == expected.Value && row.IsOpen)
                .ExecuteUpdateAsync(set => set.SetProperty(row => row.Touch, row => row.Touch + 1), cancellationToken) == 1;
            if (admitted) return null;
            var row = await database.Set<NamespaceMarkerRow>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == 1, cancellationToken);
            return NamespaceCommitRejected.ForMarker(row is null ? null : new(new(row.Incarnation), row.IsOpen, row.Touch), expected);
        }
        catch (Exception error) when (MissingMarkerTable(error))
        {
            // An unmigrated or removed namespace cannot admit a commit. Do not recreate its marker.
            return new();
        }
    }

    static bool MissingMarkerTable(Exception error) => error switch
    {
        PostgresException postgres => postgres.SqlState == PostgresErrorCodes.UndefinedTable,
        SqlException sql => sql.Number == 208,
        SqliteException sqlite => sqlite.SqliteErrorCode == 1 && sqlite.Message.Contains("no such table", StringComparison.Ordinal),
        _ => false
    };

    async Task<bool> Insert(NamespaceIncarnationId incarnation)
    {
        await using var scope = await open();
        scope.DbContext.Set<NamespaceMarkerRow>().Add(new() { Incarnation = incarnation.Value, IsOpen = true });
        try
        {
            await scope.DbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException error) when (error.IsUniqueViolation())
        {
            return false;
        }
    }
}
