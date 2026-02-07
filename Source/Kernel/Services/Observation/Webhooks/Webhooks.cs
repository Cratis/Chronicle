// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Grains.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Reactive;
using ProtoBuf.Grpc;
using WebhookDefinition = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDefinition;

namespace Cratis.Chronicle.Services.Observation.Webhooks;

/// <summary>
/// Represents an implementation of <see cref="IWebhooks"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="storage"><see cref="IStorage"/> for getting webhook definitions.</param>
internal sealed class Webhooks(IGrainFactory grainFactory, IStorage storage) : IWebhooks
{
    /// <inheritdoc/>
    public async Task Add(AddWebhooks request, CallContext context = default)
    {
        var eventSequence = grainFactory.GetEventLog();

        foreach (var webhook in request.Webhooks)
        {
            var chronicleWebhook = webhook.ToChronicle();
            var @event = new Grains.Observation.Webhooks.WebhookAdded(
                chronicleWebhook.Identifier,
                chronicleWebhook.Owner,
                chronicleWebhook.EventSequenceId,
                chronicleWebhook.EventTypes,
                chronicleWebhook.Target,
                chronicleWebhook.IsReplayable,
                chronicleWebhook.IsActive);

            await eventSequence.Append(webhook.Identifier, @event);
        }
    }

    /// <inheritdoc/>
    public async Task Remove(RemoveWebhooks request, CallContext context = default)
    {
        var eventSequence = grainFactory.GetEventLog();

        foreach (var webhookId in request.Webhooks)
        {
            var @event = new Grains.Observation.Webhooks.WebhookRemoved();
            await eventSequence.Append(webhookId, @event);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<WebhookDefinition>> GetWebhooks(GetWebhooksRequest request)
    {
        var definitions = await storage.GetEventStore(request.EventStore).Webhooks.GetAll();
        return definitions.Select(definition => definition.ToContract());
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<WebhookDefinition>> ObserveWebhooks(GetWebhooksRequest request, CallContext context = default) =>
        storage.GetEventStore(request.EventStore)
            .Webhooks
            .ObserveAll()
            .CompletedBy(context.CancellationToken)
            .Select(definitions => definitions.Select(definition => definition.ToContract()).ToList());
}
