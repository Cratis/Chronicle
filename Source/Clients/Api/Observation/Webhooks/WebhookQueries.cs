// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Reactive;

namespace Cratis.Chronicle.Api.Observation.Webhooks;

/// <summary>
/// Represents the API for working with webhooks.
/// </summary>
[Route("/api/event-store/{eventStore}/observers/webhooks")]
public class WebhookQueries : ControllerBase
{
    readonly IWebhooks _webhooks;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookQueries"/> class.
    /// </summary>
    /// <param name="webhooks"><see cref="IWebhooks"/> for working with webhooks.</param>
    internal WebhookQueries(IWebhooks webhooks)
    {
        _webhooks = webhooks;
    }

    /// <summary>
    /// Observes all webhooks registered.
    /// </summary>
    /// <param name="eventStore">The event stores to observe webhooks for.</param>
    /// <returns>An observable for observing a collection of <see cref="WebhookDefinition"/>.</returns>
    [HttpGet("observe")]
    public ISubject<IEnumerable<WebhookDefinition>> AllWebhooks([FromRoute] string eventStore) =>
        _webhooks.InvokeAndWrapWithSubject(token => _webhooks.ObserveWebhooks(new() { EventStore = eventStore }, token));

    /// <summary>
    /// Get all webhooks registered.
    /// </summary>
    /// <param name="eventStore">The event stores to get for.</param>
    /// <returns>A collection of <see cref="WebhookDefinition"/>.</returns>
    [HttpGet]
    public Task<IEnumerable<WebhookDefinition>> GetAllWebhooks([FromRoute] string eventStore) =>
        _webhooks.GetWebhooks(new() { EventStore = eventStore });
}
