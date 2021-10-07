// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents a projection in the system.
    /// </summary>
    public interface IProjection
    {
        /// <summary>
        /// Gets the unique identifier of the <see cref="IProjection"/>.
        /// </summary>
        ProjectionId    Identifier { get; }

        /// <summary>
        /// Provides the projection with a new <see cref="Event"/>.
        /// </summary>
        /// <param name="event"><see cref="Event"/> to provide.</param>
        /// <return>Async Task containing <see cref="Changeset"/> as result.</return>
        Task<Changeset> OnNext(Event @event);
    }
}
