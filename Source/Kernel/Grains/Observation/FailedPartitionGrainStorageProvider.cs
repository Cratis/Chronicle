// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Observation;
using Cratis.Observation;
using Orleans.Runtime;
using Orleans.Storage;

namespace Cratis.Kernel.Storage.Observation;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling <see cref="FailedPartition" /> storage.
/// </summary>
public class FailedPartitionGrainStorageProvider : IGrainStorage
{
    readonly IStorage _storage;

    /// <summary>
    /// Initializes a new instance of the <see cref="FailedPartitionGrainStorageProvider"/> class.
    /// </summary>
    /// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
    public FailedPartitionGrainStorageProvider(IStorage storage)
    {
        _storage = storage;
    }

    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<FailedPartitions>)!;
        var observerId = grainId.GetGuidKey(out var observerKeyAsString);
        var observerKey = ObserverKey.Parse(observerKeyAsString!);

        var failedPartitions = _storage.GetEventStore((string)observerKey.MicroserviceId).GetNamespace(observerKey.TenantId).FailedPartitions;
        actualGrainState.State = await failedPartitions.GetFor(observerId);
    }

    /// <inheritdoc/>
    public virtual async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<FailedPartitions>)!;
        var observerId = grainId.GetGuidKey(out var observerKeyAsString);
        var observerKey = ObserverKey.Parse(observerKeyAsString!);

        var failedPartitions = _storage.GetEventStore((string)observerKey.MicroserviceId).GetNamespace(observerKey.TenantId).FailedPartitions;
        foreach (var failedPartition in actualGrainState.State.Partitions)
        {
            failedPartition.ObserverId = observerId;
        }

        await failedPartitions.Save(observerId, actualGrainState.State);
    }
}
