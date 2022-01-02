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
        /// Handle the next event.
        /// </summary>
        /// <param name="event">The actual event.</param>
        /// <param name="eventTypes">Event types to set.</param>
        /// <returns>Awaitable task</returns>
        Task OnNext(AppendedEvent @event, IEnumerable<EventType> eventTypes);
    }
}
