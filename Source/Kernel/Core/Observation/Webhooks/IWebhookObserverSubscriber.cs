// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Defines a specialized <see cref="IObserverSubscriber"/> for projections.
/// </summary>
public interface IWebhookObserverSubscriber : IObserverSubscriber, IAmOwnedByKernel
{
    /// <summary>
    /// Waits for all preceding deliveries on this activation to finish, including cancellation cleanup.
    /// </summary>
    /// <returns>An awaitable completion barrier.</returns>
    /// <remarks>This method must remain non-interleavable.</remarks>
    Task Drain();

    /// <summary>
    /// Delivers events with cancellation of the underlying HTTP operation.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <param name="events">The events to deliver.</param>
    /// <param name="context">The subscriber context.</param>
    /// <param name="cancellationToken">Cancellation of the entire delivery.</param>
    /// <returns>The delivery result, after the HTTP operation has completed.</returns>
    Task<ObserverSubscriberResult> OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context, CancellationToken cancellationToken);
}
