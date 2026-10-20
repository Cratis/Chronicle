// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Defines a system that can create <see cref="HttpClient"/> instances for webhooks.
/// </summary>
public interface IWebhookHttpClientFactory
{
    /// <summary>
    /// Creates the <see cref="HttpClient"/> for the given <see cref="WebhookTarget"/>.
    /// </summary>
    /// <param name="webhookTarget">The <see cref="WebhookTarget"/>.</param>
    /// <param name="accessToken">Legacy access token parameter; OAuth tokens are now applied on each send.</param>
    /// <returns>The <see cref="HttpClient"/>.</returns>
    HttpClient Create(WebhookTarget webhookTarget, string? accessToken = null);
}