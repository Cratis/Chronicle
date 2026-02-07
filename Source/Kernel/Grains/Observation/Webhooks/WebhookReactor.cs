// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Grains.Observation.Reactors.Kernel;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

#pragma warning disable IDE0060 // Remove unused parameter

/// <summary>
/// Represents a reactor that handles webhook events.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="logger">The <see cref="ILogger{WebhookReactor}"/> for logging.</param>
public class WebhookReactor(IGrainFactory grainFactory, ILogger<WebhookReactor> logger) : Reactor
{
    /// <summary>
    /// Handles the addition of a webhook.
    /// </summary>
    /// <param name="event">The event containing the webhook information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task Added(WebhookAdded @event, EventContext eventContext)
    {
        logger.AddingWebhook(eventContext.EventSourceId, @event.Identifier);

        var definition = new WebhookDefinition(
            @event.Identifier,
            @event.Owner,
            @event.EventSequenceId,
            @event.EventTypes,
            @event.Target,
            @event.IsReplayable,
            @event.IsActive);

        var webhooksManager = grainFactory.GetGrain<IWebhooksManager>(eventContext.EventStore.Value);
        await webhooksManager.Add(definition);

        logger.WebhookAdded(eventContext.EventSourceId, @event.Identifier);
    }

    /// <summary>
    /// Handles the removal of a webhook.
    /// </summary>
    /// <param name="event">The event containing the webhook information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task Removed(WebhookRemoved @event, EventContext eventContext)
    {
        logger.RemovingWebhook(eventContext.EventSourceId);

        var webhookId = new WebhookId(eventContext.EventSourceId.Value);
        var webhooksManager = grainFactory.GetGrain<IWebhooksManager>(eventContext.EventStore.Value);
        await webhooksManager.Remove(webhookId);

        logger.WebhookRemoved(eventContext.EventSourceId);
    }
}
