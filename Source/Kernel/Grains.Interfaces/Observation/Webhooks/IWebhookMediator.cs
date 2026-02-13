// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Monads;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Defines a system that acts as an in-memory mediator between the actual client connected and an observer subscriber.
/// </summary>
public interface IWebhookMediator
{
    /// <summary>
    /// Notify that events should be observed.
    /// </summary>
    /// <param name="webhookTarget">The <see cref="WebhookTarget"/>.</param>
    /// <param name="partition"><see cref="Key"/> for the partition.</param>
    /// <param name="events">Collection of <see cref="AppendedEvent"/> to observe.</param>
    /// <param name="accessToken">Optional access token to use for authorization.</param>
    /// <param name="timeout">The optional timeout.</param>
    /// <returns>A <see cref="Task{T}"/> of <see cref="Catch"/>.</returns>
    Task<Catch> OnNext(WebhookTarget webhookTarget, Key partition, IEnumerable<AppendedEvent> events, string? accessToken = null, TimeSpan? timeout = null);
}
