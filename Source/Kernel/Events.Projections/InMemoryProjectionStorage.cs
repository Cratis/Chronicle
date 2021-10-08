// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionStorage"/> for working with projections in memory.
    /// </summary>
    public class InMemoryProjectionStorage : IProjectionStorage
    {
        /// <inheritdoc/>
        public Task<ExpandoObject> FindOrDefault(object key)
        {
            dynamic result = new ExpandoObject();
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task ApplyChanges(object key, Changeset changeset)
        {
            return Task.CompletedTask;
        }
    }
}
