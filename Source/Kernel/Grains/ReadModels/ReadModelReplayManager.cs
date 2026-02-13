// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.ReadModels;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModelReplayManager"/>.
/// </summary>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.ReadModelReplayManager)]
public class ReadModelReplayManager : Grain<ReadModelReplayManagerState>, IReadModelReplayManager
{
    /// <inheritdoc/>
    public Task Replayed(ObserverId observerId, ReplayContext context)
    {
        var occurrence = new ReadModelOccurrence(
            observerId,
            context.Started,
            context.Type,
            context.ContainerName,
            context.RevertContainerName);
        State.Occurrences.Add(occurrence);
        State.NewOccurrences.Add(occurrence);
        return WriteStateAsync();
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ReadModelOccurrence>> GetOccurrences() => Task.FromResult(State.Occurrences.AsEnumerable());
}
