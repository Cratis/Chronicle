// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    /// Remove a <see cref="WebhookId"/> for the event store it belongs to.
    /// </summary>
    /// <param name="webhookId">The <see cref="WebhookId"/> to remove.</param>
    /// <returns>Awaitable task.</returns>
    Task Remove(WebhookId webhookId);
}
