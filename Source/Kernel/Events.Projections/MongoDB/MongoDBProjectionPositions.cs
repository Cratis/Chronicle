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
    public class MongoDBProjectionPositions : IProjectionPositions
    {
        readonly IMongoClient _client;
        readonly IMongoDatabase _database;
        readonly IMongoCollection<ProjectionPosition> _projectionPositionsCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDBProjectionPositions"/> class.
        /// </summary>
        /// <param name="mongoDBClientFactory"><see cref="IMongoDBClientFactory"/> for accessing database.</param>
        public MongoDBProjectionPositions(IMongoDBClientFactory mongoDBClientFactory)
        {
            var mongoUrlBuilder = new MongoUrlBuilder
            {
                Servers = new[] { new MongoServerAddress("localhost", 27017) }
            };
            var url = mongoUrlBuilder.ToMongoUrl();
            var settings = MongoClientSettings.FromUrl(url);
            _client = mongoDBClientFactory.Create(settings);
            _database = _client.GetDatabase("projections");
            _projectionPositionsCollection = _database.GetCollection<ProjectionPosition>("projection-positions");
        }

        /// <inheritdoc/>
        public async Task<EventLogSequenceNumber> GetFor(IProjection projection, ProjectionResultStoreConfigurationId configurationId)
        {
            var identifier = GetIdentifierFor(projection, configurationId);
            var result = await _projectionPositionsCollection.FindAsync(_ => _.Id == identifier);
            var position = result.SingleOrDefault();
            return position?.Position ?? 1;
        }

        /// <inheritdoc/>
        public async Task Save(IProjection projection, ProjectionResultStoreConfigurationId configurationId, EventLogSequenceNumber position)
        {
            var identifier = GetIdentifierFor(projection, configurationId);
            await _projectionPositionsCollection.UpdateOneAsync(
                _ => _.Id == identifier,
                Builders<ProjectionPosition>.Update.Set(_ => _.Position, position.Value),
                new() { IsUpsert = true });
        }

        /// <inheritdoc/>
        public Task Reset(IProjection projection, ProjectionResultStoreConfigurationId configurationId)
        {
            return Save(projection, configurationId, 1);
        }

        string GetIdentifierFor(IProjection projection, ProjectionResultStoreConfigurationId configurationId) => $"{projection.Identifier}-{configurationId}";
    }
}
