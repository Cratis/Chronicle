// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation;

/// <summary>
/// Represents in-memory storage of sequence-local observer checkpoints.
/// </summary>
public class ObserverSequenceCheckpointsStorage : IObserverSequenceCheckpointsStorage
{
    readonly ConcurrentDictionary<(ObserverId ObserverId, EventSequenceId EventSequenceId), ObserverSequenceCheckpoint> _checkpoints = new();

    /// <inheritdoc/>
    public Task<ObserverSequenceCheckpoint?> Get(ObserverId observerId, EventSequenceId eventSequenceId) =>
        Task.FromResult(_checkpoints.TryGetValue((observerId, eventSequenceId), out var checkpoint) ? checkpoint : null);

    /// <inheritdoc/>
    public Task Save(ObserverSequenceCheckpoint checkpoint)
    {
        _checkpoints[(checkpoint.ObserverId, checkpoint.EventSequenceId)] = checkpoint;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveAllFor(ObserverId observerId)
    {
        foreach (var key in _checkpoints.Keys.Where(key => key.ObserverId == observerId))
        {
            _checkpoints.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }
}
