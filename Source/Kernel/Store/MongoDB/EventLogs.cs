// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using ExecutionContext = Aksio.Cratis.Execution.ExecutionContext;

namespace Aksio.Cratis.Events.Store.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventLogs"/> for MongoDB.
    /// </summary>
    [SingletonPerTenant]
    public class EventLogs : IEventLogs
    {
        readonly ILogger<EventLogs> _logger;
        readonly IExecutionContextManager _executionContextManager;
        readonly IEventStoreDatabase _eventStoreDatabase;
        readonly IEventConverter _converter;

        /// <summary>
        /// Initializes a new instance of <see cref="EventLogs"/>.
        /// </summary>
        /// <param name="eventStoreDatabase"><see cref="ProviderFor{T}">Provider for</see> <see cref="IMongoDatabase"/>.</param>
        /// <param name="converter"><see cref="IEventConverter"/> to convert event types.</param>
        /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for getting current <see cref="ExecutionContext"/>.</param>
        /// <param name="logger"><see cref="ILogger"/> for logging.</param>
        public EventLogs(
            IEventStoreDatabase eventStoreDatabase,
            IEventConverter converter,
            IExecutionContextManager executionContextManager,
            ILogger<EventLogs> logger)
        {
            _eventStoreDatabase = eventStoreDatabase;
            _converter = converter;
            _logger = logger;
            _executionContextManager = executionContextManager;
        }

        /// <inheritdoc/>
        public async Task Append(EventLogId eventLogId, EventLogSequenceNumber sequenceNumber, EventSourceId eventSourceId, EventType eventType, JsonObject content)
        {
            try
            {
                _logger.Appending(sequenceNumber);
                var @event = new Event(
                    sequenceNumber,
                    _executionContextManager.Current.CorrelationId,
                    _executionContextManager.Current.CausationId,
                    _executionContextManager.Current.CausedBy,
                    eventType.Id,
                    DateTimeOffset.UtcNow,
                    eventSourceId,
                    new Dictionary<string, BsonDocument>
                    {
                        { eventType.Generation.ToString(), BsonDocument.Parse(content.ToJsonString()) }
                    },
                    Array.Empty<EventCompensation>());
                await GetCollectionFor(eventLogId).InsertOneAsync(@event);
            }
            catch (Exception ex)
            {
                _logger.AppendFailure(ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public Task Compensate(EventLogId eventLogId, EventLogSequenceNumber sequenceNumber, EventType eventType, JsonObject content) => throw new NotImplementedException();

        /// <inheritdoc/>
        public async Task<IEventCursor> FindFor(EventLogId eventLogId, EventSourceId? eventSourceId = default)
        {
            var collection = _eventStoreDatabase.GetEventLogCollectionFor(eventLogId);
            var filter = eventSourceId == default ?
                Builders<Event>.Filter.Empty :
                Builders<Event>.Filter.Eq(_ => _.EventSourceId, eventSourceId);

            var cursor = await collection.Find(filter).ToCursorAsync();
            return new EventCursor(_converter, cursor);
        }

        IMongoCollection<Event> GetCollectionFor(EventLogId eventLogId) => _eventStoreDatabase.GetEventLogCollectionFor(eventLogId);
    }
}
