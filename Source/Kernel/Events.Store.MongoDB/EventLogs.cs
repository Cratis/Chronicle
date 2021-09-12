// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Cratis.DependencyInversion;
using Cratis.Execution;

namespace Cratis.Events.Store.MongoDB
{

    /// <summary>
    /// Represents an implementation of <see cref="IEventLogs"/> for MongoDB.
    /// </summary>
    [SingletonPerTenant]
    public class EventLogs : IEventLogs
    {
        const string BaseCollectionName = "event-log";
        readonly ILogger<EventLogs> _logger;
        readonly ProviderFor<IMongoDatabase> _mongoDatabaseProvider;

        static EventLogs()
        {
            BsonClassMap.RegisterClassMap<Event>(cm =>
            {
                cm.AutoMap();
                cm.MapIdProperty(_ => _.SequenceNumber);
            });
        }

        /// <summary>
        /// Initializes a new instance of <see cref="EventLogs"/>.
        /// </summary>
        /// <param name="mongoDatabaseProvider"><see cref="ProviderFor{T}">Provider for</see> <see cref="IMongoDatabase"/>.</param>
        /// <param name="logger"><see cref="ILogger"/> for logging.</param>
        public EventLogs(ProviderFor<IMongoDatabase> mongoDatabaseProvider, ILogger<EventLogs> logger)
        {
            _mongoDatabaseProvider = mongoDatabaseProvider;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task Commit(EventLogId eventLogId, EventLogSequenceNumber sequenceNumber, EventSourceId eventSourceId, EventType eventType, string content)
        {
            try
            {
                _logger.Committing(sequenceNumber);
                var @event = new Event
                {
                    SequenceNumber = sequenceNumber,
                    Type = eventType.EventTypeId,
                    Occurred = DateTimeOffset.UtcNow,
                    EventSourceId = eventSourceId,
                };
                @event.Content[eventType.Generation.ToString()] = BsonDocument.Parse(content);
                await GetCollectionFor(eventLogId).InsertOneAsync(@event);
            }
            catch (Exception ex)
            {
                _logger.CommitFailure(ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public Task<IEventStoreFindResult> FindFor(EventLogId eventLogId, EventSourceId eventSourceId)
        {
            return Task.FromResult<IEventStoreFindResult>(null!);
        }

        IMongoCollection<Event> GetCollectionFor(EventLogId eventLogId)
        {
            var collectionName = BaseCollectionName;
            if (!eventLogId.IsDefault)
            {
                if (eventLogId.IsPublic)
                {
                    collectionName = $"{BaseCollectionName}-public";
                }
                else
                {
                    collectionName = $"{BaseCollectionName}-{eventLogId}";
                }
            }

            return _mongoDatabaseProvider().GetCollection<Event>(collectionName);
        }
    }
}
