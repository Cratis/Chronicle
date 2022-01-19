// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans;

namespace Aksio.Cratis.Events.Store.Grains.Observation
{
    /// <summary>
    /// Defines a partitioned observer.
    /// </summary>
    public interface IPartitionedObserver : IGrainWithGuidCompoundKey
    {
        /// <summary>
        /// Try to resume the partition.
        /// </summary>
        /// <returns>Awaitable task.</returns>
        Task TryResume();

        /// <summary>
        /// Set the current connection identifier.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <returns>Awaitable task.</returns>
        Task SetConnectionId(string connectionId);

        /// <summary>
        /// Handle the next event.
        /// </summary>
        /// <param name="event">The actual event.</param>
        /// <param name="eventTypes">Event types to set.</param>
        /// <returns>Awaitable task.</returns>
        Task OnNext(AppendedEvent @event, IEnumerable<EventType> eventTypes);
    }
}
