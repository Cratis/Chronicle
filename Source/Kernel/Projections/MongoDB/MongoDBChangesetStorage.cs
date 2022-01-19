// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events.Projections.Changes;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Events.Projections.MongoDB
{
    /// <summary>
    /// Represents a <see cref="IChangesetStorage"/> for storing changesets in MongoDB.
    /// </summary>
    public class MongoDBChangesetStorage : IChangesetStorage
    {
        /// <inheritdoc/>
        public Task Save(CorrelationId correlationId, IEnumerable<IChangeset<Event, ExpandoObject>> associatedChangesets)
        {
            return Task.CompletedTask;
        }
    }
}
