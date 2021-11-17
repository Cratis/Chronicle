// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Projections;
using MongoDB.Driver;
using Event = Cratis.Events.Projections.Event;

namespace Cratis.Extensions.Dolittle.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventCursor"/> for handling Dolittle events from event log.
    /// </summary>
    public class EventCursor : IEventCursor
    {
        readonly IAsyncCursor<EventStore.Event>? _innerCursor;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventCursor"/> class.
        /// </summary>
        /// <param name="innerCursor"></param>
        public EventCursor(IAsyncCursor<EventStore.Event>? innerCursor)
        {
            _innerCursor = innerCursor;
        }

        /// <inheritdoc/>
        public IEnumerable<Event> Current { get; private set; } = Array.Empty<Event>();

        /// <inheritdoc/>
        public async Task<bool> MoveNext()
        {
            if (_innerCursor is null) return false;
            var result = await _innerCursor.MoveNextAsync();
            if (_innerCursor.Current is not null)
            {
                Current = _innerCursor.Current.Select(@event => @event.ToCratis()).ToArray();
            }
            else
            {
                Current = Array.Empty<Event>();
            }
            return result;
        }
    }
}
