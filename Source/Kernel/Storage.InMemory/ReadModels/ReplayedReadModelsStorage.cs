// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Storage.InMemory.ReadModels;

/// <summary>
/// Represents an in-memory implementation of <see cref="IReplayedReadModelsStorage"/>.
/// </summary>
public sealed class ReplayedReadModelsStorage : IReplayedReadModelsStorage
{
    readonly ConcurrentDictionary<ReadModelIdentifier, List<ReadModelOccurrence>> _occurrences = new();

    /// <inheritdoc/>
    public Task Replayed(ReadModelOccurrence occurrence)
    {
        var occurrences = _occurrences.GetOrAdd(occurrence.Type.Identifier, _ => []);
        lock (occurrences)
        {
            occurrences.Add(occurrence);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ReadModelOccurrence>> GetOccurrences(ReadModelIdentifier readModel)
    {
        if (!_occurrences.TryGetValue(readModel, out var occurrences))
        {
            return Task.FromResult<IEnumerable<ReadModelOccurrence>>([]);
        }

        lock (occurrences)
        {
            return Task.FromResult<IEnumerable<ReadModelOccurrence>>(occurrences.ToArray());
        }
    }

    /// <inheritdoc/>
    public Task Remove(ReadModelOccurrence occurrence)
    {
        if (!_occurrences.TryGetValue(occurrence.Type.Identifier, out var occurrences))
        {
            return Task.CompletedTask;
        }

        lock (occurrences)
        {
            occurrences.RemoveAll(_ =>
                _.ObserverId == occurrence.ObserverId &&
                _.Occurred == occurrence.Occurred &&
                _.RevertContainerName == occurrence.RevertContainerName);

            if (occurrences.Count == 0)
            {
                _occurrences.TryRemove(occurrence.Type.Identifier, out _);
            }
        }

        return Task.CompletedTask;
    }
}
