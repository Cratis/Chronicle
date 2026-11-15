// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Orleans.Storage;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for the <see cref="WebhookSigningKeyRingState"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class WebhookSigningKeyRingStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<WebhookSigningKeyRingState>)!;
        var eventStore = storage.GetEventStore(grainId.Key.ToString()!);
        actualGrainState.State!.Keys = await eventStore.WebhookSigningKeys.GetAll();
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<WebhookSigningKeyRingState>)!;
        var eventStore = storage.GetEventStore(grainId.Key.ToString()!);
        foreach (var key in actualGrainState.State!.Keys)
        {
            await eventStore.WebhookSigningKeys.Save(key);
        }
    }
}
