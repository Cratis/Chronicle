// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;
using Aksio.Cratis.Kernel.Grains.Clients;
using Aksio.Cratis.MongoDB;
using MongoDB.Driver;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aksio.Cratis.Kernel.MongoDB.Clients;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling connected clients storage.
/// </summary>
public class ConnectedClientsStorageProvider : IGrainStorage
{
    readonly ISharedDatabase _database;
    IMongoCollection<MongoDBConnectedClientsForMicroserviceState> Collection => _database.GetCollection<MongoDBConnectedClientsForMicroserviceState>(CollectionNames.ConnectedClients);

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectedClientsStorageProvider"/> class.
    /// </summary>
    /// <param name="database">The shared database.</param>
    public ConnectedClientsStorageProvider(ISharedDatabase database)
    {
        _database = database;
    }

    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ConnectedClientsState>)!;
        var microserviceId = (MicroserviceId)grainId.GetGuidKey();
        var cursor = await Collection.FindAsync(_ => _.Id == microserviceId).ConfigureAwait(false);
        var mongoState = await cursor.FirstOrDefaultAsync().ConfigureAwait(false);

        actualGrainState.State = new ConnectedClientsState
        {
            Clients = new List<ConnectedClient>(mongoState?.Clients ?? Enumerable.Empty<ConnectedClient>())
        };
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ConnectedClientsState>)!;
        var microserviceId = (MicroserviceId)grainId.GetGuidKey();
        var state = actualGrainState.State;
        var mongoState = new MongoDBConnectedClientsForMicroserviceState(microserviceId, state.Clients);
        await Collection.ReplaceOneAsync(_ => _.Id == microserviceId, mongoState, options: new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
    }
}
