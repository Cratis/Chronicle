// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Observation;
using MongoDB.Driver;

namespace Aksio.Cratis.Events.Store.MongoDB.Observation
{
    /// <summary>
    /// Represents an implementation of <see cref="IFailedObservers"/>.
    /// </summary>
    public class FailedObservers : IFailedObservers
    {
        readonly IEventStoreDatabase _eventStoreDatabase;

        IMongoCollection<FailedObserverState> Collection => _eventStoreDatabase.GetCollection<FailedObserverState>(CollectionNames.FailedObservers);

        /// <summary>
        /// Initializes a new instance of the <see cref="FailedObservers"/> class.
        /// </summary>
        /// <param name="eventStoreDatabase"><see cref="IEventStoreDatabase"/> to work with.</param>
        public FailedObservers(IEventStoreDatabase eventStoreDatabase)
        {
            _eventStoreDatabase = eventStoreDatabase;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<FailedObserverState>> GetAll() => (await Collection.FindAsync(FilterDefinition<FailedObserverState>.Empty)).ToList();
    }
}
