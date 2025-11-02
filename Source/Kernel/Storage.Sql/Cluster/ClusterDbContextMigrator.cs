// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Represents a hosted service that runs migrations for the <see cref="ClusterDbContext"/> on startup.
/// </summary>
/// <param name="dbContextFactory">The <see cref="IDbContextFactory{TContext}"/> for creating <see cref="ClusterDbContext"/> instances.</param>
public class ClusterDbContextMigrator(IDbContextFactory<ClusterDbContext> dbContextFactory) : IHostedService
{
    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
