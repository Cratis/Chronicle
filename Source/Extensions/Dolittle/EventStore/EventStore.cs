// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Extensions.MongoDB;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Cratis.Extensions.Dolittle.EventStore
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventStore"/>.
    /// </summary>
    public class EventStore : IEventStore
    {
        readonly IMongoClient _client;
        readonly IMongoDatabase _database;
        readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStore"/> class.
        /// </summary>
        /// <param name="mongoDBClientFactory"><see cref="IMongoDBClientFactory"/> for connecting to MongoDB.</param>
        /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
        public EventStore(IMongoDBClientFactory mongoDBClientFactory, ILoggerFactory loggerFactory)
        {
            var mongoUrlBuilder = new MongoUrlBuilder
            {
                Servers = new[] { new MongoServerAddress("localhost", 27017) }
            };
            var url = mongoUrlBuilder.ToMongoUrl();
            var settings = MongoClientSettings.FromUrl(url);
            _client = mongoDBClientFactory.Create(settings);
            _database = _client.GetDatabase("event_store");
            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc/>
        public IEventStream GetStream(EventStreamId id)
        {
            if (id.Equals(EventStreamId.EventLog))
            {
                return new EventStream(_database.GetCollection<Event>("event-log"), _loggerFactory.CreateLogger<EventStream>());
            }

            throw new NotImplementedException("Event log is the only stream supported at this point");
        }
    }
}
