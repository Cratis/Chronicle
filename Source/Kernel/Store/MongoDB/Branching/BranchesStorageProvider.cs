// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events.Store.Branching;
using MongoDB.Driver;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aksio.Cratis.Events.Store.MongoDB.Branching;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling the aggregation of branches.
/// </summary>
public class BranchesStorageProvider : IGrainStorage
{
    readonly ProviderFor<IEventStoreDatabase> _eventStoreDatabaseProvider;

    IMongoCollection<BranchState> Collection => _eventStoreDatabaseProvider().GetCollection<BranchState>(CollectionNames.Branches);

    /// <summary>
    /// Initializes a new instance of the <see cref="BranchesStorageProvider"/> class.
    /// </summary>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreDatabase"/>.</param>
    public BranchesStorageProvider(ProviderFor<IEventStoreDatabase> eventStoreDatabaseProvider)
    {
        _eventStoreDatabaseProvider = eventStoreDatabaseProvider;
    }

    /// <inheritdoc/>
    public Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        var cursor = await Collection.FindAsync(_ => true);
        var branches = await cursor.ToListAsync();
        grainState.State = new BranchesState(branches);
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        var state = (grainState.State as BranchesState)!;
        var cursor = await Collection.FindAsync(_ => true);
        var existingBranches = await cursor.ToListAsync();
        var branchesToDelete = existingBranches.Where(_ => state.Branches.Any(b => b.Identifier == _.Identifier)).ToArray();

        var operations = new List<WriteModel<BranchState>>();
        operations.AddRange(branchesToDelete.Select(_ => new DeleteOneModel<BranchState>(
            Builders<BranchState>.Filter.Where(f => f.Identifier == _.Identifier))));

        operations.AddRange(state.Branches.Select(_ => new ReplaceOneModel<BranchState>(
            Builders<BranchState>.Filter.Where(f => f.Identifier == _.Identifier), _)
            { IsUpsert = true }));

        await Collection.BulkWriteAsync(operations);
    }
}
