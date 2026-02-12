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
    /// Add projections.
    /// </summary>
    /// <param name="request">The <see cref="AddWebhooks"/> holding the registration.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Add(AddWebhooks request, CallContext context = default);

    /// <summary>
    /// Remove projections.
    /// </summary>
    /// <param name="request">The <see cref="RemoveWebhooks"/> request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Remove(RemoveWebhooks request, CallContext context = default);

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

    /// <summary>
    /// Test OAuth authorization.
    /// </summary>
    /// <param name="request"><see cref="TestOAuthAuthorizationRequest"/>.</param>
    /// <param name="context"><see cref="CallContext"/>.</param>
    /// <returns><see cref="TestOAuthAuthorizationResponse"/>.</returns>
    [Operation]
    Task<TestOAuthAuthorizationResponse> TestOAuthAuthorization(TestOAuthAuthorizationRequest request, CallContext context = default);
}
