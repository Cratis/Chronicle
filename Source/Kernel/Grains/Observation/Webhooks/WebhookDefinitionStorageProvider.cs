// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Storage;
using Orleans.Storage;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling webhook state storage.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class WebhookDefinitionStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var webhookKey = WebhookKey.Parse(grainId.Key.ToString()!);
        var eventStore = storage.GetEventStore(webhookKey.EventStore);
        return eventStore.Webhooks.Delete(webhookKey.WebhookId);
    }

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<WebhookDefinition>)!;
        var webhookKey = WebhookKey.Parse(grainId.Key.ToString()!);
        var eventStore = storage.GetEventStore(webhookKey.EventStore);

        if (!await eventStore.Webhooks.Has(webhookKey.WebhookId)) return;
        actualGrainState.State = await eventStore.Webhooks.Get(webhookKey.WebhookId);
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<WebhookDefinition>)!;
        var webhookKey = WebhookKey.Parse(grainId.Key.ToString()!);
        var eventStore = storage.GetEventStore(webhookKey.EventStore);
        await eventStore.Webhooks.Save(actualGrainState.State);
    }
}
