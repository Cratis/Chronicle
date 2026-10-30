// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;
using Cratis.Chronicle.Concepts.Clustering;
using Cratis.Chronicle.Storage.SingleWriter;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Stores the single-writer lease in the cluster database.
/// </summary>
/// <param name="database">The migration-aware database.</param>
public class SqlSingleWriterLeaseStorage(IDatabase database) : ISingleWriterLeaseStorage
{
    /// <inheritdoc/>
    public async Task<SingleWriterLeaseAttempt> TryAcquire(SingleWriterLeaseClaim claim, CancellationToken cancellationToken)
    {
        await using var scope = await database.Cluster();
        var context = scope.DbContext;
        var acquired = await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            // Keep the SQL Server range lock across both statements, including an absent row.
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var rows = await SingleWriterLeaseSql.Acquire(context, claim, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return rows == 1;
        });
        var row = await context.Set<SingleWriterLeaseRow>().AsNoTracking().SingleOrDefaultAsync(lease => lease.Name == "kernel", cancellationToken);
        var holder = row is null ? null : new SingleWriterLeaseHolder(new(row.Owner), row.HostName, row.Version, Utc(row.AcquiredAt), Utc(row.ExpiresAt));
        return new(acquired, holder);
    }

    /// <inheritdoc/>
    public async Task<bool> Renew(SingleWriterLeaseOwner owner, TimeSpan ttl, CancellationToken cancellationToken)
    {
        await using var scope = await database.Cluster();
        return await scope.DbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            await SingleWriterLeaseSql.Renew(scope.DbContext, owner.Value, ttl, cancellationToken) == 1);
    }

    /// <inheritdoc/>
    public async Task Release(SingleWriterLeaseOwner owner, CancellationToken cancellationToken)
    {
        await using var scope = await database.Cluster();
        await scope.DbContext.Database.CreateExecutionStrategy().ExecuteAsync(() => SingleWriterLeaseSql.Release(scope.DbContext, owner.Value, cancellationToken));
    }

    static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
