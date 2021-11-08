// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Changes;
using Cratis.Execution;

namespace Cratis.Events.Projections.Changes
{
    /// <summary>
    /// Represents a null <see cref="IChangesetStorage"/> that does nothing.
    /// </summary>
    public class NullChangesetStorage : IChangesetStorage
    {
        /// <inheritdoc/>
        public Task Save(CorrelationId correlationId, IEnumerable<Changeset<Event>> associatedChangesets) => Task.CompletedTask;
    }
}
