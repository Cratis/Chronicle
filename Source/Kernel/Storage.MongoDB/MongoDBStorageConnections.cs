// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Connections;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IMongoDBStorageConnections"/>.
/// </summary>
/// <remarks>
/// Without configured connection resolution every database is on the configured server, under the name Chronicle
/// derives — exactly as before. With it, a resolved connection string replaces the server, a resolved database the
/// derived name, and a resolved identity becomes the credential: a user name and password for SCRAM, an access token
/// for MONGODB-OIDC, refreshed by the driver through the resolver. The cluster database always stays the configured
/// one; only its identity is resolved.
/// </remarks>
/// <param name="mongoDBOptions">The <see cref="IOptions{TOptions}"/> for <see cref="MongoDBOptions"/>.</param>
/// <param name="resolution">The <see cref="IStorageConnectionResolution"/>.</param>
/// <param name="clientManager">The <see cref="IMongoDBClientManager"/>.</param>
public class MongoDBStorageConnections(
    IOptions<MongoDBOptions> mongoDBOptions,
    IStorageConnectionResolution resolution,
    IMongoDBClientManager clientManager) : IMongoDBStorageConnections
{
    readonly ConcurrentDictionary<string, MongoDBConnectionResolutionHooks> _hooks = new();

    /// <inheritdoc/>
    public IMongoDatabase GetDatabase(StorageConnectionKind kind, string derivedName, string eventStore = "", string @namespace = "")
    {
        var configured = new MongoUrl(mongoDBOptions.Value.Server);
        if (!resolution.IsConfigured)
        {
            return clientManager.GetClientFor(WithDirectConnection(MongoClientSettings.FromUrl(configured))).GetDatabase(derivedName);
        }

        var resolver = resolution.Resolver;
        var request = resolution.RequestFor(ConnectionPurpose.Runtime, kind, eventStore, @namespace);

        // Databases are opened from synchronous paths. The resolution is cached, so this only waits when it is not.
        var resolved = resolution.Resolve(ConnectionPurpose.Runtime, kind, eventStore, @namespace).GetAwaiter().GetResult();

        var hooks = _hooks.GetOrAdd(request.ToKey(), static (_, state) => new(state.Resolver, state.Request, TimeProvider.System), (Resolver: resolver, Request: request));
        var settings = WithDirectConnection(resolved.ToMongoClientSettings(configured, hooks));
        settings.ClusterConfigurator = hooks.ConfigureCluster;

        return clientManager.GetClientFor(settings).GetDatabase(resolved.Database ?? derivedName);
    }

    MongoClientSettings WithDirectConnection(MongoClientSettings settings)
    {
        if (mongoDBOptions.Value.DirectConnection == true)
        {
            settings.DirectConnection = true;
        }

        return settings;
    }
}
