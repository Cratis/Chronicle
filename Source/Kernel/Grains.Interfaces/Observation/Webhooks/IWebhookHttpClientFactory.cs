// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Defines a system that can create <see cref="HttpClient"/> instances for a given <see cref="WebhookDefinition"/>.
/// </summary>
public interface IWebhookHttpClientFactory
{
    /// <summary>
    /// Creates the <see cref="HttpClient"/> for the given <see cref="WebhookDefinition"/>.
    /// </summary>
    /// <param name="definition">The <see cref="WebhookDefinition"/>.</param>
    /// <returns>The <see cref="HttpClient"/>.</returns>
    HttpClient Create(WebhookDefinition definition);
}