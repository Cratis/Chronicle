// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Defines a projection.
    /// </summary>
    public interface IProjector
    {
        /// <summary>
        /// Gets the <see cref="IObservable{T}">observable</see> <see cref="EventContext">event</see>.
        /// </summary>
        IObservable<EventContext> Event { get; }

        /// <summary>
        /// Provides the projection with a new <see cref="Event"/>.
        /// </summary>
        /// <param name="event"><see cref="Event"/> to provide.</param>
        /// <param name="storage"><see cref="IProjectionStorage"/> to use.</param>
        /// <return>Async Task containing <see cref="Changeset"/> as result.</return>
        Task<Changeset> OnNext(Event @event, IProjectionStorage storage);
    }
}
