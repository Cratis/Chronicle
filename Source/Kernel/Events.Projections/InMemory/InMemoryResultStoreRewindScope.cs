// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.InMemory
{
    /// <summary>
    /// Represents a <see cref="IProjectionResultStoreRewindScope"/> for in-memory.
    /// </summary>
    public class InMemoryResultStoreRewindScope : IProjectionResultStoreRewindScope
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryResultStoreRewindScope"/> class.
        /// </summary>
        /// <param name="model"><see cref="Model"/> the scope is for.</param>
        public InMemoryResultStoreRewindScope(Model model) => Model = model;

        /// <inheritdoc/>
        public Model Model { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
