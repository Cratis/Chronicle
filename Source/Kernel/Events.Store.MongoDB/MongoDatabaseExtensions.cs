// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Events.Store.MongoDB
{
    /// <summary>
    /// Extension methods for working with <see cref="IMongoDatabase"/>.
    /// </summary>
    public static class MongoDatabaseExtensions
    {
        const string BaseCollectionName = "event-log";

        /// <summary>
        /// Get the <see cref="IMongoCollection{T}"/> for an event log based on identifier.
        /// </summary>
        /// <param name="database"><see cref="IMongoDatabase"/> to get from.</param>
        /// <param name="eventLogId"><see cref="EventLogId"/> identifier.</param>
        /// <returns>The collection instance.</returns>
        public static IMongoCollection<Event> GetEventLogCollectionFor(this IMongoDatabase database, EventLogId eventLogId)
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

            return database.GetCollection<Event>(collectionName);

        }
    }
}
