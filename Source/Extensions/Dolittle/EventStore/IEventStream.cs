// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dolittle.SDK.Events;
using MongoDB.Driver;

namespace Cratis.Extensions.Dolittle.EventStore
{
    /// <summary>
    /// Defines an API for working with a Dolittle event stream.
    /// </summary>
    public interface IEventStream
    {
        /// <summary>
        /// Count number of events in the stream.
        /// </summary>
        /// <returns>The count of events.</returns>
        long Count();

        /// <summary>
        /// Watch for changes on the event stream.
        /// </summary>
        /// <returns><see cref="IChangeStreamCursor{T}"/> for changes.</returns>
        IChangeStreamCursor<ChangeStreamDocument<Event>> Watch();

        /// <summary>
        /// Get events from a specific position.
        /// </summary>
        /// <param name="position">Position to get from.</param>
        /// <param name="eventTypes">Optional event types to get. If not specified, all will be given.</param>
        /// <param name="eventSourceId">Optional <see cref="EventSourceId"/>.</param>
        /// <returns><see cref="IAsyncCursor{Event}"/> for events in the stream.</returns>
        Task<IAsyncCursor<Event>> GetFromPosition(uint position, IEnumerable<EventType>? eventTypes = default, EventSourceId? eventSourceId = default);
    }
}
