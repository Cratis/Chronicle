// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Represents a startup task that runs migrations for the <see cref="ClusterDbContext"/> on startup.
/// </summary>
/// <remarks>
/// Goes through <see cref="IDatabase.Cluster"/>, so the cluster database is prepared exactly as on any other access:
/// created only when storage may create it, in the configured schema, and as the resolved schema identity.
/// </remarks>
/// <param name="database">The <see cref="IDatabase"/>.</param>
public class ClusterDbContextMigrator(IDatabase database) : IStartupTask
{
    /// <inheritdoc/>
    public async Task Execute(CancellationToken cancellationToken)
    {
        await using var scope = await database.Cluster();
    }
}
