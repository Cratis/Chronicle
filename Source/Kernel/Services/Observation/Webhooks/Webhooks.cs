// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
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
    public async Task Register(RegisterWebhook request, CallContext context = default)
    {
        var webhooksManager = grainFactory.GetGrain<Grains.Observation.Webhooks.IWebhooksManager>(request.EventStore);
        var webhooks = request.Webhooks.Select(w => w.ToChronicle()).ToArray();

        await webhooksManager.Register(webhooks);
    }

    /// <inheritdoc/>
    public async Task Unregister(UnregisterWebhook request, CallContext context = default)
    {
        var webhooksManager = grainFactory.GetGrain<Grains.Observation.Webhooks.IWebhooksManager>(request.EventStore);

        await webhooksManager.Unregister(request.Webhooks.Select(id => new WebhookId(id)));
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
