// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Grains.Observation.Reactors.Kernel;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

#pragma warning disable IDE0060 // Remove unused parameter

/// <summary>
/// Represents a reactor that handles webhook events.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="logger">The <see cref="ILogger{WebhookReactor}"/> for logging.</param>
[Reactor(eventSequence: WellKnownEventSequences.System, systemEventStoreOnly: false, defaultNamespaceOnly: true)]
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
        logger.AddingWebhook(eventContext.EventSourceId, new WebhookId(eventContext.EventSourceId.Value));

        var webhookId = new WebhookId(eventContext.EventSourceId.Value);

        var definition = new WebhookDefinition(
            webhookId,
            @event.Owner,
            @event.EventSequenceId,
            @event.EventTypes,
            new WebhookTarget(@event.TargetUrl, WebhookAuthorization.None, @event.TargetHeaders),
            @event.IsReplayable,
            @event.IsActive);

        var webhooksManager = grainFactory.GetGrain<IWebhooks>(eventContext.EventStore.Value);
        await webhooksManager.Add(definition);

        logger.WebhookAdded(eventContext.EventSourceId, webhookId);
    }

    /// <summary>
    /// Handles basic authorization being set for a webhook.
    /// </summary>
    /// <param name="event">The event containing the authorization information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task BasicAuthorizationSet(BasicAuthorizationSetForWebhook @event, EventContext eventContext)
    {
        var webhookId = new WebhookId(eventContext.EventSourceId.Value);
        logger.SettingBasicAuthorizationForWebhook(webhookId);

        var webhooksManager = grainFactory.GetGrain<IWebhooks>(eventContext.EventStore.Value);
        await webhooksManager.SetAuthorization(webhookId, new BasicAuthorization(@event.Username, @event.Password));
    }

    /// <summary>
    /// Handles bearer token authorization being set for a webhook.
    /// </summary>
    /// <param name="event">The event containing the authorization information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task BearerTokenAuthorizationSet(BearerTokenAuthorizationSetForWebhook @event, EventContext eventContext)
    {
        var webhookId = new WebhookId(eventContext.EventSourceId.Value);
        logger.SettingBearerTokenAuthorizationForWebhook(webhookId);

        var webhooksManager = grainFactory.GetGrain<IWebhooks>(eventContext.EventStore.Value);
        await webhooksManager.SetAuthorization(webhookId, new BearerTokenAuthorization(@event.Token));
    }

    /// <summary>
    /// Handles OAuth authorization being set for a webhook.
    /// </summary>
    /// <param name="event">The event containing the authorization information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task OAuthAuthorizationSet(OAuthAuthorizationSetForWebhook @event, EventContext eventContext)
    {
        var webhookId = new WebhookId(eventContext.EventSourceId.Value);
        logger.SettingOAuthAuthorizationForWebhook(webhookId);

        var webhooksManager = grainFactory.GetGrain<IWebhooks>(eventContext.EventStore.Value);
        await webhooksManager.SetAuthorization(webhookId, new OAuthAuthorization(@event.Authority, @event.ClientId, @event.ClientSecret));
    }

    /// <summary>
    /// Handles event types being set for a webhook.
    /// </summary>
    /// <param name="event">The event containing the event types.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task EventTypesSet(EventTypesSetForWebhook @event, EventContext eventContext)
    {
        var webhookId = new WebhookId(eventContext.EventSourceId.Value);
        logger.SettingEventTypesForWebhook(webhookId);

        var webhooksManager = grainFactory.GetGrain<IWebhooks>(eventContext.EventStore.Value);
        await webhooksManager.SetEventTypes(webhookId, @event.EventTypes);
    }

    /// <summary>
    /// Handles target URL being set for a webhook.
    /// </summary>
    /// <param name="event">The event containing the target URL.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task TargetUrlSet(TargetUrlSetForWebhook @event, EventContext eventContext)
    {
        var webhookId = new WebhookId(eventContext.EventSourceId.Value);
        logger.SettingTargetUrlForWebhook(webhookId);

        var webhooksManager = grainFactory.GetGrain<IWebhooks>(eventContext.EventStore.Value);
        await webhooksManager.SetTargetUrl(webhookId, @event.TargetUrl);
    }

    /// <summary>
    /// Handles target headers being set for a webhook.
    /// </summary>
    /// <param name="event">The event containing the target headers.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task TargetHeadersSet(TargetHeadersSetForWebhook @event, EventContext eventContext)
    {
        var webhookId = new WebhookId(eventContext.EventSourceId.Value);
        logger.SettingTargetHeadersForWebhook(webhookId);

        var webhooksManager = grainFactory.GetGrain<IWebhooks>(eventContext.EventStore.Value);
        await webhooksManager.SetTargetHeaders(webhookId, @event.TargetHeaders);
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
        var webhooksManager = grainFactory.GetGrain<IWebhooks>(eventContext.EventStore.Value);
        await webhooksManager.Remove(webhookId);

        logger.WebhookRemoved(eventContext.EventSourceId);
    }
}
