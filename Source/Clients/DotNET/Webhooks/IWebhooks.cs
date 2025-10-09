// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Webhooks;

/// <summary>
/// Defines a system for working with webhook registrations for the Kernel.
/// </summary>
public interface IWebhooks
{
    /// <summary>
    /// Registers a webhook.
    /// </summary>
    /// <param name="webhookId">The <see cref="WebhookId"/> of the webhook to register.</param>
    /// <param name="configure">The <see cref="Action{T}"/> for configuring the <see cref="WebhookDefinition"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Register(WebhookId webhookId, Action<IWebhookDefinitionBuilder> configure);

    /// <summary>
    /// Registers a webhook.
    /// </summary>
    /// <param name="webhook">The <see cref="WebhookDefinition"/> to register.</param>
    /// <returns>Awaitable task.</returns>
    Task Register(WebhookDefinition webhook);

    /// <summary>
    /// Get any failed partitions for a specific webhook.
    /// </summary>
    /// <param name="webhookId">The <see cref="WebhookId"/>.</param>
    /// <returns>Collection of <see cref="FailedPartition"/>, if any.</returns>
    Task<IEnumerable<FailedPartition>> GetFailedPartitionsFor(WebhookId webhookId);

    /// <summary>
    /// Get the state of a specific webhook observer.
    /// </summary>
    /// <param name="webhookId">The <see cref="WebhookId"/>.</param>
    /// <returns><see cref="WebhookState"/>.</returns>
    Task<WebhookState> GetStateFor(WebhookId webhookId);

    /// <summary>
    /// Replay a specific webhook by its identifier.
    /// </summary>
    /// <param name="webhookId"><see cref="WebhookId"/> to replay.</param>
    /// <returns>Awaitable task.</returns>
    Task Replay(WebhookId webhookId);
}
