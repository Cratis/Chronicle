// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;

/// <summary>
/// Removes persisted unhashed claims and repairs their value indexes without consulting silo-local migration caches.
/// </summary>
internal static class UnhashedUniqueConstraintValues
{
    /// <summary>
    /// Gets the case-sensitive predicate shared by cleanup and SQL Server column narrowing.
    /// </summary>
    /// <param name="context">The provider context.</param>
    /// <returns>The SQL predicate identifying unhashed values.</returns>
    internal static string Predicate(DbContext context)
    {
        var value = context.GetService<ISqlGenerationHelper>().DelimitIdentifier("Value");
        if (context.Database.IsSqlServer())
        {
            return $"DATALENGTH({value}) <> 128 OR {value} COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9a-f]%'";
        }

        return context.Database.IsNpgsql()
            ? $"{value} !~ '^[0-9a-f]{{64}}$'"
            : $"length({value}) <> 64 OR length(CAST({value} AS BLOB)) <> 64 OR {value} GLOB '*[^0-9a-f]*'";
    }

    /// <summary>
    /// Deletes legacy rows and repairs the table under one writer lock and transaction.
    /// </summary>
    /// <param name="context">The context owning the namespace connection.</param>
    /// <param name="tableName">The catalog-discovered table.</param>
    /// <param name="cancellationToken">Cancellation of the cleanup.</param>
    /// <returns>The deleted claim count.</returns>
    internal static async Task<long> RemoveFrom(DbContext context, string tableName, CancellationToken cancellationToken)
    {
        var table = context.GetService<ISqlGenerationHelper>().DelimitIdentifier(tableName);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await UniqueConstraintValueIndexMigration.LockTable(context, table);
#pragma warning disable EF1002 // Discovered identifiers are delimited by the provider; the predicate contains no user input.
        var deleted = await context.Database.ExecuteSqlRawAsync($"DELETE FROM {table} WHERE {Predicate(context)}", cancellationToken);
#pragma warning restore EF1002
        // Always perform DDL, even if this silo cached an earlier skipped migration.
        await UniqueConstraintValueIndexMigration.EnsureValueIndexIsUnique(
            tableName, context, context.GetService<ILoggerFactory>().CreateLogger<UniqueConstraintMigrator>());
        await transaction.CommitAsync(cancellationToken);
        return deleted;
    }
}
