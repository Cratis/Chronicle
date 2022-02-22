// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Defines a system for working with available <see cref="IProjectionEventProvider">projection event provider</see>.
    /// </summary>
    public interface IProjectionEventProviders
    {
        /// <summary>
        /// Check if there is a <see cref="IProjectionEventProvider"/> of a specific <see cref="ProjectionEventProviderTypeId"/> registered in the system.
        /// </summary>
        /// <param name="typeId"><see cref="ProjectionEventProviderTypeId"/> to check for.</param>
        /// <returns>True if it exists, false if not.</returns>
        bool HasType(ProjectionEventProviderTypeId typeId);

        /// <summary>
        /// Get a <see cref="IProjectionEventProvider"/> of a specific <see cref="ProjectionEventProviderTypeId"/>.
        /// </summary>
        /// <param name="typeId"><see cref="ProjectionEventProviderTypeId"/> to get for.</param>
        /// <returns><see cref="IProjectionEventProvider"/> instance.</returns>
        IProjectionEventProvider GetForType(ProjectionEventProviderTypeId typeId);
    }
}
