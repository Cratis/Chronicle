// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Observation;
using Aksio.Cratis.MongoDB;
using MongoDB.Driver;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aksio.Cratis.Events.Store.MongoDB.Observation;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling client observer state storage.
/// </summary>
public class ClientObserversStorageProvider : IGrainStorage
{
    readonly IClusterDatabase _database;

    IMongoCollection<ClientObserversState> Collection => _database.GetCollection<ClientObserversState>();


    /// <summary>
    /// Initializes a new instance of the <see cref="ClientObserversStorageProvider"/> class.
    /// </summary>
    /// <param name="database"><see cref="IClusterDatabase"/> for persistance.</param>
    public ClientObserversStorageProvider(IClusterDatabase database)
    {
        _database = database;
    }

    /// <inheritdoc/>
    public Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        var id = Guid.Empty.ToString();
        var cursor = await Collection.FindAsync(_ => _.Id == id);
        grainState.State = await cursor.FirstOrDefaultAsync() ?? new ClientObserversState();
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        var id = Guid.Empty.ToString();
        var state = grainState.State as ClientObserversState;
        state!.Id = id;
        await Collection.ReplaceOneAsync(
            _ => _.Id == id,
            state!,
            new ReplaceOptions { IsUpsert = true });
    }
}
