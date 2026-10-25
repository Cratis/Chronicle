// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.Integration.SqlClustering.RollingUpgrade.ObserverStorageRoundtrip;

/// <summary>
/// Uses Testcontainers infrastructure readiness to await the old kernel's persisted, specifically subscribed observer.
/// </summary>
/// <param name="database">The isolated PostgreSQL store.</param>
public sealed class ReleasedSqlObserverReadyWait(SqlClusterDatabase database) : IWaitUntil
{
    /// <summary>
    /// Gets the selected namespace connection, observer identifier, and event sequence owner.
    /// </summary>
    public (string ConnectionString, string Identifier, string Owner)? Observer { get; private set; }

    /// <inheritdoc/>
    public async Task<bool> UntilAsync(IContainer container)
    {
        await using var connection = database.CreateConnection();
        Observer = await ReleasedObserverDatabase.FindObserver(connection.ConnectionString);
        return Observer is not null;
    }
}
