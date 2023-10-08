// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectedClientsStorageProvider"/> class.
    /// </summary>
    /// <param name="database">The shared database.</param>
    public ConnectedClientsStorageProvider(ISharedDatabase database)
    {
        _database = database;
    }

    IMongoCollection<ConnectedClientsState> Collection => _database.GetCollection<ConnectedClientsState>(CollectionNames.ConnectedClients);

    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<Grains.Clients.ConnectedClientsState>)!;
        var cursor = await Collection.FindAsync(_ => _.Id == 0).ConfigureAwait(false);
        var mongoState = await cursor.FirstOrDefaultAsync().ConfigureAwait(false);

        actualGrainState.State = new Grains.Clients.ConnectedClientsState
        {
            Clients = new List<ConnectedClient>(mongoState?.Clients ?? Enumerable.Empty<ConnectedClient>())
        };
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<Grains.Clients.ConnectedClientsState>)!;
        var state = actualGrainState.State;
        var mongoState = new ConnectedClientsState(0, state.Clients);
        await Collection.ReplaceOneAsync(_ => _.Id == 0, mongoState, options: new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
    }
}
