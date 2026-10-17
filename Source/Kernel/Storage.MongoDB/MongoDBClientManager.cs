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
    readonly ConcurrentDictionary<(string Servers, MongoCredential? Credential), Lazy<IMongoClient>> _clients = new();

    /// <inheritdoc/>
    /// <remarks>
    /// Clients are shared per set of servers and credential, so settings that only differ in who they authenticate as
    /// never share a client.
    /// </remarks>
    public IMongoClient GetClientFor(MongoClientSettings settings)
    {
        var identifier = (string.Join('#', settings.Servers.Select(_ => _.ToString()).Order()), settings.Credential);
        return _clients.GetOrAdd(
            identifier,
            static (_, state) => new Lazy<IMongoClient>(() => state.Factory.Create(state.Settings), LazyThreadSafetyMode.ExecutionAndPublication),
            (Factory: clientFactory, Settings: settings)).Value;
    }
}
