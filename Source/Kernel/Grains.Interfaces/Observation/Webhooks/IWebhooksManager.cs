// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Defines a system that is responsible for supervises webhooks in the system.
/// </summary>
public interface IWebhooksManager : IGrainWithStringKey
{
    /// <summary>
    /// Ensure the existence of the webhook manager.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Ensure();

    /// <summary>
    /// Get all the <see cref="WebhookDefinition">webhook definitions</see> available.
    /// </summary>
    /// <returns>A collection of <see cref="WebhookDefinition"/>.</returns>
    Task<IEnumerable<WebhookDefinition>> GetWebhookDefinitions();

    /// <summary>
    /// Add a <see cref="WebhookDefinition"/> for the event store it belongs to.
    /// </summary>
    /// <param name="definition">The <see cref="WebhookDefinition"/> to add.</param>
    /// <returns>Awaitable task.</returns>
    Task Add(WebhookDefinition definition);

    /// <summary>
    /// Update a <see cref="WebhookDefinition"/> for the event store it belongs to.
    /// </summary>
    /// <param name="definition">The <see cref="WebhookDefinition"/> to update.</param>
    /// <returns>Awaitable task.</returns>
    Task Update(WebhookDefinition definition);

    /// <summary>
    /// Set the authorization for a webhook.
    /// </summary>
    /// <param name="webhookId">The <see cref="WebhookId"/>.</param>
    /// <param name="authorization">The <see cref="WebhookAuthorization"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task SetAuthorization(WebhookId webhookId, WebhookAuthorization authorization);

    /// <summary>
    /// Set the event types for a webhook.
    /// </summary>
    /// <param name="webhookId">The <see cref="WebhookId"/>.</param>
    /// <param name="eventTypes">The event types.</param>
    /// <returns>Awaitable task.</returns>
    Task SetEventTypes(WebhookId webhookId, IEnumerable<EventType> eventTypes);

    /// <summary>
    /// Set the target URL for a webhook.
    /// </summary>
    /// <param name="webhookId">The <see cref="WebhookId"/>.</param>
    /// <param name="targetUrl">The <see cref="WebhookTargetUrl"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task SetTargetUrl(WebhookId webhookId, WebhookTargetUrl targetUrl);

    /// <summary>
    /// Set the target headers for a webhook.
    /// </summary>
    /// <param name="webhookId">The <see cref="WebhookId"/>.</param>
    /// <param name="headers">The target headers.</param>
    /// <returns>Awaitable task.</returns>
    Task SetTargetHeaders(WebhookId webhookId, IReadOnlyDictionary<string, string> headers);

    /// <summary>
    /// Remove a <see cref="WebhookId"/> for the event store it belongs to.
    /// </summary>
    /// <param name="webhookId">The <see cref="WebhookId"/> to remove.</param>
    /// <returns>Awaitable task.</returns>
    Task Remove(WebhookId webhookId);
}
