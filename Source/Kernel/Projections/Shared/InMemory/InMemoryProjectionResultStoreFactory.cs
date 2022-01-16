// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.InMemory
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionResultStoreFactory"/> for <see cref="InMemoryProjectionResultStore"/>.
    /// </summary>
    public class InMemoryProjectionResultStoreFactory : IProjectionResultStoreFactory
    {
        /// <inheritdoc/>
        public ProjectionResultStoreTypeId TypeId => InMemoryProjectionResultStore.ProjectionResultStoreTypeId;

        /// <inheritdoc/>
        public IProjectionResultStore CreateFor(Model model) => new InMemoryProjectionResultStore(model);
    }
}
