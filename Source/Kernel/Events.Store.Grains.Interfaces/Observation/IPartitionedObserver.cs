// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Copyright (c) Cratis. All rights reserved.

using Orleans;

namespace Cratis.Events.Store.Grains.Observation
{
    /// <summary>
    /// Defines a partitioned observer.
    /// </summary>
    public interface IPartitionedObserver : IGrainWithGuidCompoundKey
    {
        /// <summary>
        /// Set event types for the observer.
        /// </summary>
        /// <param name="eventTypes">Event types to set.</param>
        /// <returns>Awaitable task</returns>
        Task SetEventTypes(IEnumerable<EventType> eventTypes);

        /// <summary>
        /// Handle the next event.
        /// </summary>
        /// <param name="event">The actual event.</param>
        /// <returns>Awaitable task</returns>
        Task OnNext(AppendedEvent @event);
    }
}
