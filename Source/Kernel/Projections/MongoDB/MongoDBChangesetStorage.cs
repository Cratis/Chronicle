// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events.Projections.Changes;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Events.Projections.MongoDB
{
    /// <summary>
    /// Represents a <see cref="IChangesetStorage"/> for storing changesets in MongoDB.
    /// </summary>
    public class MongoDBChangesetStorage : IChangesetStorage
    {
        /// <inheritdoc/>
        public Task Save(CorrelationId correlationId, IChangeset<AppendedEvent, ExpandoObject> associatedChangeset)
        {
            return Task.CompletedTask;
        }
    }
}
