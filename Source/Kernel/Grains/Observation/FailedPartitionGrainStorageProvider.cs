// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactions;
using Orleans.Runtime;
using Orleans.Storage;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling <see cref="FailedPartition" /> storage.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FailedPartitionGrainStorageProvider"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class FailedPartitionGrainStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<FailedPartitions>)!;
        var observerKey = ObserverKey.Parse(grainId.Key.ToString()!);

        var failedPartitions = storage.GetEventStore(observerKey.EventStore).GetNamespace(observerKey.Namespace).FailedPartitions;
        actualGrainState.State = await failedPartitions.GetFor(observerKey.ObserverId);
    }

    /// <inheritdoc/>
    public virtual async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<FailedPartitions>)!;
        var observerKey = ObserverKey.Parse(grainId.Key.ToString()!);

        var failedPartitions = storage.GetEventStore(observerKey.EventStore).GetNamespace(observerKey.Namespace).FailedPartitions;
        foreach (var failedPartition in actualGrainState.State.Partitions)
        {
            failedPartition.ObserverId = observerKey.ObserverId;
        }

        await failedPartitions.Save(observerKey.ObserverId, actualGrainState.State);
    }
}
