// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Events.Projections
{

    /// <summary>
    /// Defines a projection.
    /// </summary>
    public interface IProjection
    {
        /// <summary>
        /// Gets the unique identifier of the <see cref="IProjection"/>.
        /// </summary>
        ProjectionId    Identifier { get; }

        /// <summary>
        /// Gets the <see cref="IModel"/> the projection targets.
        /// </summary>
        IModel Model { get; }

        /// <summary>
        /// Gets the <see cref="IObservable{T}">observable</see> <see cref="EventContext">event</see>.
        /// </summary>
        IObservable<EventContext> Event { get; }

        /// <summary>
        /// Gets the <see cref="EventType">event types</see> the projection can handle.
        /// </summary>
        IEnumerable<EventType> EventTypes { get; }

        /// <summary>
        /// Provides the projection with a new <see cref="Event"/>.
        /// </summary>
        /// <param name="event"><see cref="Event"/> to provide.</param>
        /// <param name="initialState"><see cref="ExpandoObject"/> holding the initial state before the event is applied.</param>
        /// <return><see cref="Changeset"/> with all changes.</return>
        Changeset OnNext(Event @event, ExpandoObject initialState);

        /// <summary>
        /// Get the <see cref="KeyResolver"/> associated with a given <see cref="EventType"/>.
        /// </summary>
        /// <param name="eventType"><see cref="EventType"/> to get for.</param>
        /// <returns>The <see cref="KeyResolver"/>.</returns>
        KeyResolver GetKeyResolverFor(EventType eventType);
    }
}
