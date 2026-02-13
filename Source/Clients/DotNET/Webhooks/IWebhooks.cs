// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    /// <param name="targetUrl">The <see cref="WebhookTargetUrl"/>.</param>
    /// <param name="configure">The <see cref="Action{T}"/> for configuring the <see cref="WebhookDefinition"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Register(WebhookId webhookId, WebhookTargetUrl targetUrl, Action<IWebhookDefinitionBuilder> configure);
}
