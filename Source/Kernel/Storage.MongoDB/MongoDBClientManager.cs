// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.MongoDB;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IMongoDBClientManager"/>.
/// </summary>
public class MongoDBClientManager : IMongoDBClientManager
{
    readonly IMongoDBClientFactory _clientFactory;
    readonly ConcurrentDictionary<string, IMongoClient> _clients = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBClientManager"/> class.
    /// </summary>
    /// <param name="clientFactory"><see cref="IMongoDBClientFactory"/> to use for creating clients.</param>
    public MongoDBClientManager(IMongoDBClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    /// <inheritdoc/>
    public IMongoClient GetClientFor(MongoClientSettings settings)
    {
        var identifier = string.Join('#', settings.Servers.Select(_ => _.ToString()).OrderBy(_ => _));
        if (!_clients.TryGetValue(identifier, out var client))
        {
            _clients[identifier] = client = _clientFactory.Create(settings);
        }

        return client;
    }
}
