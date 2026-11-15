// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents the MongoDB connections of named sink configurations, independent of the kernel's event storage.
/// </summary>
/// <remarks>
/// A named MongoDB sink connects to its own server with its own client, so read models can be stored in MongoDB whatever engine the
/// kernel keeps its events in. One client is kept per connection string for the lifetime of the process.
/// </remarks>
/// <param name="options">The <see cref="ChronicleOptions"/> holding the sink configurations.</param>
public class NamedSinkDatabases(ChronicleOptions options)
{
    readonly ConcurrentDictionary<string, IMongoClient> _clients = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<(string Sink, EventStoreName EventStore, EventStoreNamespaceName Namespace), IMongoDatabase> _databases = new();

    /// <summary>
    /// Get the read model database of an event store namespace in a named sink.
    /// </summary>
    /// <param name="sink">The <see cref="SinkName"/> of the configuration.</param>
    /// <param name="eventStore">The <see cref="EventStoreName"/>.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/>.</param>
    /// <returns>The <see cref="IMongoDatabase"/> the read models of the namespace are stored in.</returns>
    /// <exception cref="SinkTypeDoesNotSupportNamedConfigurations">Thrown when the named configuration is not a MongoDB sink.</exception>
    public IMongoDatabase GetReadModelDatabase(SinkName sink, EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        if (!options.Sinks.TryGet(sink.Value, out var key, out var configuration) ||
            !string.Equals(configuration.Type, WellKnownSinkTypes.MongoDB.Value, StringComparison.OrdinalIgnoreCase))
        {
            throw new SinkTypeDoesNotSupportNamedConfigurations(WellKnownSinkTypes.MongoDB, sink);
        }

        return _databases.GetOrAdd(
            (key.ToLowerInvariant(), eventStore, @namespace),
            static (target, state) =>
            {
                var client = state.Clients.GetOrAdd(state.ConnectionDetails, static connectionString => new MongoClient(MongoClientSettings.FromUrl(new MongoUrl(connectionString))));
                var databaseName = DatabaseNames.ForReadModels(target.EventStore, target.Namespace, state.Storage.DatabaseNamePrefix, state.Storage.SystemDatabaseName);
                return client.GetDatabaseByExistingCasing(databaseName);
            },
            (Clients: _clients, configuration.ConnectionDetails, options.Storage));
    }
}
