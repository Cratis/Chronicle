// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInversion;
using Cratis.Execution;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using ExecutionContext = Cratis.Execution.ExecutionContext;

namespace Cratis.Events.Store.MongoDB
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

        /// <summary>
        /// Initializes a new instance of <see cref="EventLogs"/>.
        /// </summary>
        /// <param name="eventStoreDatabase"><see cref="ProviderFor{T}">Provider for</see> <see cref="IMongoDatabase"/>.</param>
        /// <param name="logger"><see cref="ILogger"/> for logging.</param>
        /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for getting current <see cref="ExecutionContext"/>.</param>
        public EventLogs(IEventStoreDatabase eventStoreDatabase, ILogger<EventLogs> logger, IExecutionContextManager executionContextManager)
        {
            _eventStoreDatabase = eventStoreDatabase;
            _logger = logger;
            _executionContextManager = executionContextManager;
        }

        /// <inheritdoc/>
        public async Task Append(EventLogId eventLogId, EventLogSequenceNumber sequenceNumber, EventSourceId eventSourceId, EventType eventType, string content)
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
                        { eventType.Generation.ToString(), BsonDocument.Parse(content) }
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
        public Task Compensate(EventLogId eventLogId, EventLogSequenceNumber sequenceNumber, EventType eventType, string content) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IEventStoreFindResult> FindFor(EventLogId eventLogId, EventSourceId eventSourceId)
        {
            return Task.FromResult<IEventStoreFindResult>(null!);
        }

        IMongoCollection<Event> GetCollectionFor(EventLogId eventLogId) => _eventStoreDatabase.GetEventLogCollectionFor(eventLogId);
    }
}
