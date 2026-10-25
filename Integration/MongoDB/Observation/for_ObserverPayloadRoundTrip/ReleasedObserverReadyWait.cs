// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.MongoDB;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using MongoDB.Bson;
using MongoDB.Driver;

using MongoObserverDefinition = Cratis.Chronicle.Storage.MongoDB.Observation.ObserverDefinition;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ObserverPayloadRoundTrip;

/// <summary>
/// Infrastructure readiness requires a persisted old-kernel observer, not merely an open HTTP port.
/// </summary>
/// <param name="client">Gets a client after MongoDB's mapped port is available.</param>
public sealed class ReleasedObserverReadyWait(Func<IMongoClient> client) : IWaitUntil
{
    /// <summary>
    /// Gets the discovered namespace row and its owning event sequence.
    /// </summary>
    public (string Database, string Identifier, EventSequenceId Owner)? Observer { get; private set; }

    /// <inheritdoc/>
    public async Task<bool> UntilAsync(IContainer container)
    {
        var mongoDB = client();
        using var names = await mongoDB.ListDatabaseNamesAsync();
        var databases = await names.ToListAsync();
        foreach (var storeName in databases.Where(name => name.EndsWith("+es", StringComparison.Ordinal)))
        {
            var definitions = await mongoDB.GetDatabase(storeName).GetCollection<MongoObserverDefinition>(WellKnownCollectionNames.Observers)
                .Find(definition => definition.Owner == ObserverOwner.Kernel).ToListAsync();
            foreach (var definition in definitions.Where(definition => definition.EventTypes.Count > 0))
            {
                foreach (var namespaceName in databases.Where(name => name.StartsWith($"{storeName}+", StringComparison.Ordinal)))
                {
                    if (await mongoDB.GetDatabase(namespaceName).GetCollection<BsonDocument>(WellKnownCollectionNames.Observers)
                        .Find(Builders<BsonDocument>.Filter.Eq("_id", definition.Id.Value)).AnyAsync())
                    {
                        Observer = (namespaceName, definition.Id.Value, definition.EventSequenceId);
                        return true;
                    }
                }
            }
        }
        return false;
    }
}
