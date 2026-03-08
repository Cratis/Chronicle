// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines the base interface for observer subscribers.
/// </summary>
public interface IObserverSubscriber : IGrainWithStringKey
{
    /// <summary>
    /// Called whenever an event is ready to be observed.
    /// </summary>
    /// <param name="partition">The <see cref="Key"/> for the partition.</param>
    /// <param name="events">A collection of <see cref="AppendedEvent"/>.</param>
    /// <param name="context">The <see cref="ObserverSubscriberContext"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task<ObserverSubscriberResult> OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context);
}
