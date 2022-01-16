// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Events.Projections.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventCursor"/> for handling events from event log.
    /// </summary>
    public class EventCursor : IEventCursor
    {
        readonly IAsyncCursor<Store.MongoDB.Event>? _innerCursor;

        /// <inheritdoc/>
        public IEnumerable<Event> Current { get; private set; } = Array.Empty<Event>();

        /// <summary>
        /// Initializes a new instance of the <see cref="EventCursor"/> class.
        /// </summary>
        /// <param name="innerCursor">The underlying MongoDB cursor.</param>
        public EventCursor(IAsyncCursor<Store.MongoDB.Event>? innerCursor)
        {
            _innerCursor = innerCursor;
        }

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
