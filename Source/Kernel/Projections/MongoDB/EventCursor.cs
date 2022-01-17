// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Compliance;
using Cratis.Events.Schemas;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace Cratis.Events.Projections.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventCursor"/> for handling events from event log.
    /// </summary>
    public class EventCursor : IEventCursor
    {
        readonly ISchemaStore _schemaStore;
        readonly IJsonComplianceManager _jsonComplianceManager;
        readonly IAsyncCursor<Store.MongoDB.Event>? _innerCursor;

        /// <inheritdoc/>
        public IEnumerable<Event> Current { get; private set; } = Array.Empty<Event>();

        /// <summary>
        /// Initializes a new instance of the <see cref="EventCursor"/> class.
        /// </summary>
        /// <param name="schemaStore"><see cref="ISchemaStore"/> for event schemas.</param>
        /// <param name="jsonComplianceManager"><see cref="IJsonComplianceManager"/> for handling compliance on events.</param>
        /// <param name="innerCursor">The underlying MongoDB cursor.</param>
        public EventCursor(
            ISchemaStore schemaStore,
            IJsonComplianceManager jsonComplianceManager,
            IAsyncCursor<Store.MongoDB.Event>? innerCursor)
        {
            _schemaStore = schemaStore;
            _jsonComplianceManager = jsonComplianceManager;
            _innerCursor = innerCursor;
        }

        /// <inheritdoc/>
        public async Task<bool> MoveNext()
        {
            if (_innerCursor is null) return false;
            var result = await _innerCursor.MoveNextAsync();
            if (_innerCursor.Current is not null)
            {
                Current = await Task.WhenAll(_innerCursor.Current.Select(@event => ConvertToCratis(@event)));
            }
            else
            {
                Current = Array.Empty<Event>();
            }
            return result;
        }

        async Task<Event> ConvertToCratis(Store.MongoDB.Event @event)
        {
            var eventType = new EventType(@event.Type, EventGeneration.First);
            var content = @event.Content[EventGeneration.First.ToString()].ToString();
            var eventSchema = await _schemaStore.GetFor(eventType.Id, eventType.Generation);
            var releasedContent = await _jsonComplianceManager.Release(eventSchema.Schema, @event.EventSourceId, JObject.Parse(content));

            return new Event(
                @event.SequenceNumber,
                eventType,
                @event.Occurred,
                @event.EventSourceId,
                BsonSerializer.Deserialize<ExpandoObject>(releasedContent.ToString()!));
        }
    }
}
