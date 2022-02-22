// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Defines a factory that can create instances of <see cref="IProjectionSink"/> for a specific <see cref="ProjectionSinkTypeId"/>.
    /// </summary>
    public interface IProjectionSinkFactory
    {
        /// <summary>
        /// Gets the <see cref="ProjectionSinkTypeId"/> that identifies the type of store the factory supports.
        /// </summary>
        ProjectionSinkTypeId TypeId { get; }

        /// <summary>
        /// Create a <see cref="IProjectionSink"/> for a specific <see cref="Model"/>.
        /// </summary>
        /// <param name="model"><see cref="Model"/> to create for.</param>
        /// <returns>A new instance of <see cref="IProjectionSink"/> for the <see cref="Model"/>.</returns>
        IProjectionSink CreateFor(Model model);
    }
}
