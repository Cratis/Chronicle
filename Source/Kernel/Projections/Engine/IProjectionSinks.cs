// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Defines a system for working with available <see cref="IProjectionSink">projection sinks</see>.
    /// </summary>
    public interface IProjectionSinks
    {
        /// <summary>
        /// Check if there is a <see cref="IProjectionSink"/> of a specific <see cref="ProjectionSinkTypeId"/> registered in the system.
        /// </summary>
        /// <param name="typeId"><see cref="ProjectionSinkTypeId"/> to check for.</param>
        /// <returns>True if it exists, false if not.</returns>
        bool HasType(ProjectionSinkTypeId typeId);

        /// <summary>
        /// Get a <see cref="IProjectionSink"/> of a specific <see cref="ProjectionSinkTypeId"/>.
        /// </summary>
        /// <param name="typeId"><see cref="ProjectionSinkTypeId"/> to get for.</param>
        /// <param name="model"><see cref="Model"/> to get for.</param>
        /// <returns><see cref="IProjectionSink"/> instance.</returns>
        IProjectionSink GetForTypeAndModel(ProjectionSinkTypeId typeId, Model model);
    }
}
