// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans;

namespace Cratis.Events.Store.Grains.Observation
{
    /// <summary>
    /// Defines an observer of an event log.
    /// </summary>
    public interface IObserver : IGrainWithGuidCompoundKey
    {
        /// <summary>
        /// Subscribe to observer.
        /// </summary>
        /// <param name="connectionId">The unique identifier of the connected clients connection.</param>
        /// <param name="eventTypes">Collection of <see cref="EventType">event types</see> to subscribe to.</param>
        /// <returns>Awaitable task.</returns>
        Task Subscribe(string connectionId, IEnumerable<EventType> eventTypes);

        /// <summary>
        /// Unsubscribe from the observer.
        /// </summary>
        /// <param name="subscriptionId">Subscription to unsubscribe.</param>
        /// <returns>Awaitable task.</returns>
        Task Unsubscribe(Guid subscriptionId);
    }
}
