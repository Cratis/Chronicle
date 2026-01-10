// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.Webhooks;

/// <summary>
/// Defines the contract for working with client webhooks.
/// </summary>
[Service]
public interface IWebhooks
{
    /// <summary>
    /// Register projections.
    /// </summary>
    /// <param name="request">The <see cref="RegisterWebhook"/> holding the registration.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Register(RegisterWebhook request, CallContext context = default);

    /// <summary>
    /// Unregister projections.
    /// </summary>
    /// <param name="request">The <see cref="UnregisterWebhook"/> request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Unregister(UnregisterWebhook request, CallContext context = default);

    /// <summary>
    /// Gets all webhooks.
    /// </summary>
    /// <param name="request"><see cref="GetWebhooksRequest"/>.</param>
    /// <returns><see cref="IEnumerable{T}"/> of <see cref="WebhookDefinition"/>.</returns>
    [Operation]
    Task<IEnumerable<WebhookDefinition>> GetWebhooks(GetWebhooksRequest request);

    /// <summary>
    /// Gets observer over all webhooks.
    /// </summary>
    /// <param name="request"><see cref="GetWebhooksRequest"/>.</param>
    /// <param name="context"><see cref="CallContext"/>.</param>
    /// <returns><see cref="IObservable{T}"/> of <see cref="IEnumerable{T}"/> of <see cref="WebhookDefinition"/>.</returns>
    [Operation]
    IObservable<IEnumerable<WebhookDefinition>> ObserveWebhooks(GetWebhooksRequest request, CallContext context = default);
}
