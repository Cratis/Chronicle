// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Defines a system that acts as an in-memory mediator between the actual client connected and an observer subscriber.
/// </summary>
public interface IWebhookMediator
{
    /// <summary>
    /// Notify that events should be observed.
    /// </summary>
    /// <param name="eventStore">The event store, or null for global policy.</param>
    /// <param name="webhookTarget">The <see cref="WebhookTarget"/>.</param>
    /// <param name="partition"><see cref="Key"/> for the partition.</param>
    /// <param name="events">Collection of <see cref="AppendedEvent"/> to observe.</param>
    /// <param name="accessToken">Optional access token to use for authorization.</param>
    /// <param name="timeout">The optional timeout.</param>
    /// <param name="cancellationToken">Cancellation of the entire delivery, including the resilience pipeline.</param>
    /// <returns>A <see cref="Task{T}"/> of <see cref="Catch"/>.</returns>
    Task<Catch> OnNext(EventStoreName? eventStore, WebhookTarget webhookTarget, Key partition, IEnumerable<AppendedEvent> events, string? accessToken = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Notify that events should be observed.
    /// </summary>
    /// <param name="eventStore">The event store, or null for global policy.</param>
    /// <param name="webhookTarget">The <see cref="WebhookTarget"/>.</param>
    /// <param name="partition"><see cref="Key"/> for the partition.</param>
    /// <param name="events">Collection of <see cref="AppendedEvent"/> to observe.</param>
    /// <param name="accessToken">Optional access token to use for authorization.</param>
    /// <param name="timeout">The optional timeout.</param>
    /// <param name="cancellationToken">Cancellation of the entire delivery, including the resilience pipeline.</param>
    /// <returns>The typed delivery outcome, including partial progress and any deadline.</returns>
    Task<WebhookDeliveryResult> Deliver(EventStoreName? eventStore, WebhookTarget webhookTarget, Key partition, IEnumerable<AppendedEvent> events, string? accessToken = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default);
}
