// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.MongoDB;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IMongoDBClientManager"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MongoDBClientManager"/> class.
/// </remarks>
/// <param name="clientFactory"><see cref="IMongoDBClientFactory"/> to use for creating clients.</param>
public class MongoDBClientManager(IMongoDBClientFactory clientFactory) : IMongoDBClientManager
{
    readonly ConcurrentDictionary<string, IMongoClient> _clients = new();

    /// <inheritdoc/>
    public IMongoClient GetClientFor(MongoClientSettings settings)
    {
        var identifier = string.Join('#', settings.Servers.Select(_ => _.ToString()).Order());
        if (!_clients.TryGetValue(identifier, out var client))
        {
            _clients[identifier] = client = clientFactory.Create(settings);
        }

        return client;
    }
}
