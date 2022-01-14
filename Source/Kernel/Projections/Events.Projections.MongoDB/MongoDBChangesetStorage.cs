// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Changes;
using Cratis.Events.Projections.Changes;
using Cratis.Execution;
using Cratis.Extensions.MongoDB;

namespace Cratis.Events.Projections.MongoDB
{
    /// <summary>
    /// Represents a <see cref="IChangesetStorage"/> for storing changesets in MongoDB.
    /// </summary>
    public class MongoDBChangesetStorage : IChangesetStorage
    {
        readonly IMongoDBClientFactory _clientFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="IMongoDBClientFactory"/>.
        /// </summary>
        /// <param name="clientFactory"></param>
        public MongoDBChangesetStorage(IMongoDBClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        /// <inheritdoc/>
        public Task Save(CorrelationId correlationId, IEnumerable<IChangeset<Event, ExpandoObject>> associatedChangesets)
        {
            return Task.CompletedTask;
        }
    }
}
