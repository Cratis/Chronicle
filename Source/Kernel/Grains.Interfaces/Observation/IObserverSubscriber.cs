// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Defines the base interface for observer subscribers.
/// </summary>
public interface IObserverSubscriber : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Called whenever an event is ready to be observed.
    /// </summary>
    /// <param name="event">The actual <see cref="AppendedEvent"/>.</param>
    /// <param name="context">The <see cref="ObserverSubscriberContext"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task<ObserverSubscriberResult> OnNext(AppendedEvent @event, ObserverSubscriberContext context);
}
