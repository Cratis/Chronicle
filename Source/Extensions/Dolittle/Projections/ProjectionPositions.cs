// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Projections;
using Cratis.Extensions.MongoDB;
using MongoDB.Driver;

namespace Cratis.Extensions.Dolittle.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionPositions"/> for Dolittle event store.
    /// </summary>
    public class ProjectionPositions : IProjectionPositions
    {
        readonly IMongoClient _client;
        readonly IMongoDatabase _database;
        readonly IMongoCollection<ProjectionPosition> _projectionPositionsCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectionPositions"/> class.
        /// </summary>
        /// <param name="mongoDBClientFactory"></param>
        public ProjectionPositions(IMongoDBClientFactory mongoDBClientFactory)
        {
            var mongoUrlBuilder = new MongoUrlBuilder
            {
                Servers = new[] { new MongoServerAddress("localhost", 27017) }
            };
            var url = mongoUrlBuilder.ToMongoUrl();
            var settings = MongoClientSettings.FromUrl(url);
            _client = mongoDBClientFactory.Create(settings);
            _database = _client.GetDatabase("event_store");
            _projectionPositionsCollection = _database.GetCollection<ProjectionPosition>("projection-positions");
        }

        /// <inheritdoc/>
        public async Task<EventLogSequenceNumber> GetFor(IProjection projection)
        {
            var result = await _projectionPositionsCollection.FindAsync(_ => _.Id == projection.Identifier.Value);
            var position = result.SingleOrDefault();
            return position?.Position ?? 0;
        }

        /// <inheritdoc/>
        public async Task Save(IProjection projection, EventLogSequenceNumber position)
        {
            await _projectionPositionsCollection.UpdateOneAsync(
                _ => _.Id == projection.Identifier.Value,
                Builders<ProjectionPosition>.Update.Set(_ => _.Position, position.Value),
                new() { IsUpsert = true });
        }

        /// <inheritdoc/>
        public Task Reset(IProjection projection)
        {
            return Save(projection, 0);
        }
    }
}
