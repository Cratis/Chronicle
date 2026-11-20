// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB;
using MongoDB.Driver;

namespace Cratis.Chronicle.Benchmarks.Storage.Hosts;

/// <summary>Creates MongoDB clients whose command monitoring feeds a <see cref="CommandCounter"/>.</summary>
/// <param name="counter">The counter.</param>
internal sealed class CountingClientManager(CommandCounter counter) : IMongoDBClientManager
{
    static readonly Dictionary<string, IMongoClient> _clients = [];

    /// <inheritdoc/>
    public IMongoClient GetClientFor(MongoClientSettings settings)
    {
        var key = $"{settings.Server}|{counter.GetHashCode()}";
        lock (_clients)
        {
            if (_clients.TryGetValue(key, out var existing)) return existing;
            settings.ClusterConfigurator = builder => builder
                .Subscribe<MongoDB.Driver.Core.Events.CommandSucceededEvent>(@event => counter.Record(@event.CommandName))
                .Subscribe<MongoDB.Driver.Core.Events.CommandFailedEvent>(@event => counter.Record(@event.CommandName));
            return _clients[key] = new MongoClient(settings);
        }
    }
}
