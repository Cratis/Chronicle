// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using MongoDB.Driver;
using KernelCheckpoint = Cratis.Chronicle.Storage.Observation.ObserverSequenceCheckpoint;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Represents MongoDB storage of sequence-local observer checkpoints.
/// </summary>
/// <param name="database">The namespace database.</param>
public class ObserverSequenceCheckpointsStorage(IEventStoreNamespaceDatabase database) : IObserverSequenceCheckpointsStorage
{
    readonly IMongoCollection<ObserverSequenceCheckpoint> _collection = database.GetCollection<ObserverSequenceCheckpoint>(WellKnownCollectionNames.ObserverSequenceCheckpoints);

    /// <inheritdoc/>
    public async Task<KernelCheckpoint?> Get(ObserverId observerId, EventSequenceId eventSequenceId)
    {
        var id = new ObserverSequenceCheckpointId(observerId, eventSequenceId);
        var document = await _collection.Find(_ => _.Id == id).FirstOrDefaultAsync().ConfigureAwait(false);
        return document?.ToKernel();
    }

    /// <inheritdoc/>
    public async Task Save(KernelCheckpoint checkpoint)
    {
        var document = checkpoint.ToMongoDB();
        await _collection.ReplaceOneAsync(_ => _.Id == document.Id, document, new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RemoveAllFor(ObserverId observerId) =>
        await _collection.DeleteManyAsync(_ => _.Id.ObserverId == observerId).ConfigureAwait(false);
}
