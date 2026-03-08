// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Represents a startup task that runs migrations for the <see cref="ClusterDbContext"/> on startup.
/// </summary>
/// <param name="dbContextFactory">The <see cref="IDbContextFactory{TContext}"/> for creating <see cref="ClusterDbContext"/> instances.</param>
public class ClusterDbContextMigrator(IDbContextFactory<ClusterDbContext> dbContextFactory) : IStartupTask
{
    /// <inheritdoc/>
    public async Task Execute(CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
    }
}
