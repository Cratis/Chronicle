// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.MongoDB;
using Aksio.Cratis.MongoDB;
using MongoDB.Driver;

namespace Aksio.Cratis.Events.Projections.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IProjectionPositions"/> for  event store.
/// </summary>
public class MongoDBProjectionPositions : IProjectionPositions
{
    readonly IEventStoreDatabase _eventStoreDatabase;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBProjectionPositions"/> class.
    /// </summary>
    /// <param name="eventStoreDatabase">The <see cref="ISharedDatabase"/>.</param>
    public MongoDBProjectionPositions(IEventStoreDatabase eventStoreDatabase)
    {
        _eventStoreDatabase = eventStoreDatabase;
    }

    /// <inheritdoc/>
    public async Task<EventLogSequenceNumber> GetFor(IProjection projection, ProjectionResultStoreConfigurationId configurationId)
    {
        var identifier = GetIdentifierFor(projection, configurationId);
        var result = await GetCollection().FindAsync(_ => _.Id == identifier);
        var position = result.SingleOrDefault();
        return position?.Position ?? EventLogSequenceNumber.First;
    }

    /// <inheritdoc/>
    public async Task Save(IProjection projection, ProjectionResultStoreConfigurationId configurationId, EventLogSequenceNumber position)
    {
        var identifier = GetIdentifierFor(projection, configurationId);
        await GetCollection().UpdateOneAsync(
            _ => _.Id == identifier,
            Builders<ProjectionPosition>.Update.Set(_ => _.Position, position.Value),
            new() { IsUpsert = true });
    }

    /// <inheritdoc/>
    public Task Reset(IProjection projection, ProjectionResultStoreConfigurationId configurationId)
    {
        return Save(projection, configurationId, EventLogSequenceNumber.First);
    }

    string GetIdentifierFor(IProjection projection, ProjectionResultStoreConfigurationId configurationId) => $"{projection.Identifier}-{configurationId}";

    IMongoCollection<ProjectionPosition> GetCollection() => _eventStoreDatabase.GetCollection<ProjectionPosition>("projection-positions");
}
