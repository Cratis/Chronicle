// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Adds command monitoring while preserving the kernel client manager cache.
/// </summary>
/// <param name="inner">The normal kernel client manager.</param>
/// <param name="counters">The scenario counters.</param>
internal sealed class ReplayShapeMongoClients(IMongoDBClientManager inner, ReplayShapeCounters counters) : IMongoDBClientManager
{
    /// <inheritdoc/>
    public IMongoClient GetClientFor(MongoClientSettings settings)
    {
        var monitored = settings.Clone();
        var configure = monitored.ClusterConfigurator;
        monitored.ClusterConfigurator = cluster =>
        {
            configure?.Invoke(cluster);
            cluster.Subscribe<CommandStartedEvent>(counters.OnCommand);
        };
        return inner.GetClientFor(monitored);
    }
}
