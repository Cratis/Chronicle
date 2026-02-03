// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Defines a projection.
/// </summary>
public interface IWebhook : IGrainWithStringKey
{
    /// <summary>
    /// Set the projection definition and subscribe as an observer.
    /// </summary>
    /// <param name="definition"><see cref="WebhookDefinition"/> to refresh with.</param>
    /// <returns>Awaitable task.</returns>
    Task SetDefinition(WebhookDefinition definition);

    /// <summary>
    /// Get the projection definition.
    /// </summary>
    /// <returns>The current <see cref="WebhookDefinition"/>.</returns>
    Task<WebhookDefinition> GetDefinition();

    /// <summary>
    /// Subscribe to changes in webhook or pipeline definition changes.
    /// </summary>
    /// <param name="subscriber"><see cref="INotifyWebhookDefinitionsChanged"/> to subscribe.</param>
    /// <returns>Awaitable task.</returns>
    Task SubscribeDefinitionsChanged(INotifyWebhookDefinitionsChanged subscriber);

    /// <summary>
    /// Unsubscribe to changes in webhook or pipeline definition changes.
    /// </summary>
    /// <param name="subscriber"><see cref="INotifyWebhookDefinitionsChanged"/> to subscribe.</param>
    /// <returns>Awaitable task.</returns>
    Task UnsubscribeDefinitionsChanged(INotifyWebhookDefinitionsChanged subscriber);
}
