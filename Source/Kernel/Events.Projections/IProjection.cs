// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Changes;
using Cratis.Properties;

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
        ProjectionId Identifier { get; }

        /// <summary>
        /// Gets the name of the projection.
        /// </summary>
        ProjectionName Name { get; }

        /// <summary>
        /// Gets the fully qualified path for the projection. Typically for child relationships, this will show the full path it applies to.
        /// </summary>
        ProjectionPath Path { get; }

        /// <summary>
        /// Gets the <see cref="Model"/> the projection targets.
        /// </summary>
        Model Model { get; }

        /// <summary>
        /// Gets the <see cref="IObservable{T}">observable</see> <see cref="EventContext">event</see>.
        /// </summary>
        IObservable<EventContext> Event { get; }

        /// <summary>
        /// Gets the <see cref="EventType">event types</see> the projection can handle.
        /// </summary>
        IEnumerable<EventType> EventTypes { get; }

        /// <summary>
        /// Gets the collection of <see cref="IProjection">child projections</see>.
        /// </summary>
        IEnumerable<IProjection> ChildProjections { get; }

        /// <summary>
        /// Apply a filter to an <see cref="IObservable{EventContext}"/> with the event types the <see cref="Projection"/> is interested in.
        /// </summary>
        /// <param name="observable"><see cref="IObservable{EventContext}"/> to filter.</param>
        /// <returns>Filtered <see cref="IObservable{EventContext}"/>.</returns>
        IObservable<EventContext> FilterEventTypes(IObservable<EventContext> observable);

        /// <summary>
        /// Apply a filter to an <see cref="IObservable{Event}"/> with the event types the <see cref="Projection"/> is interested in.
        /// </summary>
        /// <param name="observable"><see cref="IObservable{Event}"/> to filter.</param>
        /// <returns>Filtered <see cref="IObservable{Event}"/>.</returns>
        IObservable<Event> FilterEventTypes(IObservable<Event> observable);

        /// <summary>
        /// Provides the projection with a new <see cref="Event"/>.
        /// </summary>
        /// <param name="event"><see cref="Event"/> to provide.</param>
        /// <param name="changeset"><see cref="Changeset{Event, ExpandoObject}"/> being worked on.</param>
        void OnNext(Event @event, IChangeset<Event, ExpandoObject> changeset);

        /// <summary>
        /// Checks whether or not the projection will accept a specific event type.
        /// </summary>
        /// <param name="eventType"><see cref="EventType"/> to check.</param>
        /// <returns>True if it does, false if not.</returns>
        bool Accepts(EventType eventType);

        /// <summary>
        /// Get the <see cref="ValueProvider{Event}"/> associated with a given <see cref="EventType"/>.
        /// </summary>
        /// <param name="eventType"><see cref="EventType"/> to get for.</param>
        /// <returns>The <see cref="ValueProvider{Event}"/>.</returns>
        ValueProvider<Event> GetKeyResolverFor(EventType eventType);
    }
}
