// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Defines a system that can coordinate the effort around projections.
    /// </summary>
    public interface IProjectionCoordinator
    {
        /// <summary>
        /// Provides the projection with a new <see cref="Event"/>.
        /// </summary>
        /// <param name="event"><see cref="Event"/> to provide.</param>
        /// <return>Async Task containing <see cref="Changeset"/> as result.</return>
        Task<Changeset> OnNext(Event @event);
    }
}
