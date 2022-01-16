// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Defines a factory that can create instances of <see cref="IProjectionResultStore"/> for a specific <see cref="ProjectionResultStoreTypeId"/>.
    /// </summary>
    public interface IProjectionResultStoreFactory
    {
        /// <summary>
        /// Gets the <see cref="ProjectionResultStoreTypeId"/> that identifies the type of store the factory supports.
        /// </summary>
        ProjectionResultStoreTypeId TypeId { get; }

        /// <summary>
        /// Create a <see cref="IProjectionResultStore"/> for a specific <see cref="Model"/>.
        /// </summary>
        /// <param name="model"><see cref="Model"/> to create for.</param>
        /// <returns>A new instance of <see cref="IProjectionResultStore"/> for the <see cref="Model"/>.</returns>
        IProjectionResultStore CreateFor(Model model);
    }
}
