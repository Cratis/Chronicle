// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace Cratis.Chronicle.Benchmarks.Clustering;

internal sealed class CatchUpShapeMongoClients(IMongoDBClientManager inner, CatchUpShapeCounters counters) : IMongoDBClientManager
{
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
