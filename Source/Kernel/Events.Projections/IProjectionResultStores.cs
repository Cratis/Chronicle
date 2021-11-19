// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Defines a system for working with available <see cref="IProjectionResultStore">projection result stores</see>.
    /// </summary>
    public interface IProjectionResultStores
    {
        /// <summary>
        /// Check if there is a <see cref="IProjectionResultStore"/> of a specific <see cref="ProjectionResultStoreTypeId"/> registered in the system.
        /// </summary>
        /// <param name="typeId"><see cref="ProjectionResultStoreTypeId"/> to check for.</param>
        /// <returns>True if it exists, false if not.</returns>
        bool HasType(ProjectionResultStoreTypeId typeId);

        /// <summary>
        /// Get a <see cref="IProjectionResultStore"/> of a specific <see cref="ProjectionResultStoreTypeId"/>.
        /// </summary>
        /// <param name="typeId"><see cref="ProjectionResultStoreTypeId"/> to get for.</param>
        /// <returns><see cref="IProjectionResultStore"/> instance.</returns>
        IProjectionResultStore GetForType(ProjectionResultStoreTypeId typeId);
    }
}
