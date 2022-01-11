// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Extensions.Orleans.Execution;
using MongoDB.Bson;
using MongoDB.Driver;
using Orleans.Streams;

namespace Cratis.Events.Store.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="IQueueCacheCursor"/> for MongoDB event log.
    /// </summary>
    public class EventLogQueueCacheCursor : IQueueCacheCursor
    {
        readonly IMongoCollection<Event> _collection;
        readonly IStreamIdentity _streamIdentity;
        readonly IEnumerable<EventType> _eventTypes;
        readonly EventSourceId? _partition;
        IAsyncCursor<Event>? _cursor;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogQueueCacheCursor"/>.
        /// </summary>
        /// <param name="collection"><see cref="IMongoCollection{T}"/> to use for getting events from the event log.</param>
        /// <param name="streamIdentity"><see cref="IStreamIdentity"/> for the stream.</param>
        /// <param name="token"><see cref="StreamSequenceToken"/> that represents the starting point to get from.</param>
        /// <param name="eventTypes">Optional collection of <see cref="EventType">Event types</see> to filter the cursor with - default all.</param>
        /// <param name="partition">Optional <see cref="EventSourceId"/> partition to filter for.</param>
        public EventLogQueueCacheCursor(
            IMongoCollection<Event> collection,
            IStreamIdentity streamIdentity,
            StreamSequenceToken token,
            IEnumerable<EventType>? eventTypes = default,
            EventSourceId? partition = default)
        {
            _collection = collection;
            _streamIdentity = streamIdentity;
            _eventTypes = eventTypes ?? Array.Empty<EventType>();
            _partition = partition;
            FindEventsFrom(token);
        }

        /// <inheritdoc/>
        public IBatchContainer GetCurrent(out Exception exception)
        {
            exception = null!;
            if (_cursor == null) return null!;

            try
            {
                var appendedEvents = _cursor.Current.Select(_ =>
                {
                    var metadata = new EventMetadata(_.SequenceNumber, new EventType(_.Type, EventGeneration.First));
                    var context = new EventContext(_.EventSourceId, _.Occurred);
                    var content = _.Content[EventGeneration.First.Value.ToString(CultureInfo.InvariantCulture)].ToJson();
                    return new AppendedEvent(metadata, context, content);
                }).ToArray();

                if (appendedEvents.Length == 0)
                {
                    return null!;
                }

                return new EventLogBatchContainer(
                    appendedEvents,
                    _streamIdentity.Guid,
                    _streamIdentity.Namespace,
                    new Dictionary<string, object> { { RequestContextKeys.TenantId, _streamIdentity.Namespace } });
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            return null!;
        }

        /// <inheritdoc/>
        public bool MoveNext() => _cursor?.MoveNext() ?? false;

        /// <inheritdoc/>
        public void RecordDeliveryFailure()
        {
        }

        /// <inheritdoc/>
        public void Refresh(StreamSequenceToken token)
        {
            FindEventsFrom(token);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _cursor?.Dispose();
            _cursor = null!;
        }

        void FindEventsFrom(StreamSequenceToken token)
        {
            var from = (ulong)token.SequenceNumber;
            var filters = new List<FilterDefinition<Event>>
            {
                Builders<Event>.Filter.Gte(_ => _.SequenceNumber, from)
            };

            if (_partition?.IsSpecified == true)
            {
                filters.Add(Builders<Event>.Filter.Eq(e => e.EventSourceId, _partition));
            }

            if (_eventTypes.Any())
            {
                filters.Add(Builders<Event>.Filter.Or(_eventTypes.Select(_ => Builders<Event>.Filter.Eq(e => e.Type, _.Id)).ToArray()));
            }

            var filter = Builders<Event>.Filter.And(filters.ToArray());
            _cursor = _collection.Find(filter).ToCursor();
        }
    }
}
