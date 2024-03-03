// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Storage;
using Cratis.Kernel.Storage.Observation;
using Cratis.Observation;
using Orleans.Runtime;
using Orleans.Storage;

namespace Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling observer state storage.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ObserverGrainStorageProvider"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class ObserverGrainStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ObserverState>)!;
        var observerId = (ObserverId)grainId.GetGuidKey(out var observerKeyAsString);
        var observerKey = ObserverKey.Parse(observerKeyAsString!);

        var observers = storage.GetEventStore((string)observerKey.MicroserviceId).GetNamespace(observerKey.TenantId).Observers;
        actualGrainState.State = await observers.GetState(observerId, observerKey);
    }

    /// <inheritdoc/>
    public virtual async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ObserverState>)!;
        var observerId = (ObserverId)grainId.GetGuidKey(out var observerKeyAsString);
        var observerKey = ObserverKey.Parse(observerKeyAsString!);

        var observers = storage.GetEventStore((string)observerKey.MicroserviceId).GetNamespace(observerKey.TenantId).Observers;
        await observers.SaveState(observerId, observerKey, actualGrainState.State);
    }
}
