// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.Grains.Clients;
using Aksio.Cratis.MongoDB;
using MongoDB.Driver;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aksio.Cratis.Kernel.MongoDB.Clients;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling connected clients storage.
/// </summary>
public class ConnectedClientsStorageProvider : IGrainStorage
{
    readonly ISharedDatabase _database;
    IMongoCollection<MongoDBConnectedClientState> Collection => _database.GetCollection<MongoDBConnectedClientState>(CollectionNames.ConnectedClients);

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectedClientsStorageProvider"/> class.
    /// </summary>
    /// <param name="database">The shared database.</param>
    public ConnectedClientsStorageProvider(ISharedDatabase database)
    {
        _database = database;
    }

    /// <inheritdoc/>
    public Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        var microserviceId = (MicroserviceId)grainReference.GetPrimaryKey();
        var cursor = await Collection.FindAsync(_ => _.Id == microserviceId);
        var mongoState = await cursor.FirstOrDefaultAsync();

        grainState.State = new ConnectedClientsState
        {
            Clients = new List<ConnectedClient>(mongoState?.Clients ?? Enumerable.Empty<ConnectedClient>())
        };
    }

    /// <inheritdoc/>
    public Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        var microserviceId = (MicroserviceId)grainReference.GetPrimaryKey();
        var state = (grainState.State as ConnectedClientsState)!;
        var mongoState = new MongoDBConnectedClientState(microserviceId, state.Clients);
        return Collection.ReplaceOneAsync(_ => _.Id == microserviceId, mongoState, options: new ReplaceOptions { IsUpsert = true });
    }
}
