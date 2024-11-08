// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a projection.
/// </summary>
public interface IProjection
{
    /// <summary>
    /// Gets the <see cref="EventSequenceId"/> the projection is for.
    /// </summary>
    EventSequenceId EventSequenceId { get; }

    /// <summary>
    /// Gets the unique identifier of the <see cref="IProjection"/>.
    /// </summary>
    ProjectionId Identifier { get; }

    /// <summary>
    /// Gets the <see cref="SinkDefinition">sink</see> to store the results of the projection.
    /// </summary>
    SinkDefinition Sink { get; }

    /// <summary>
    /// Gets the initial state used for each model instance.
    /// </summary>
    ExpandoObject InitialModelState { get; }

    /// <summary>
    /// Gets the fully qualified path for the projection. Typically for child relationships, this will show the full path it applies to.
    /// </summary>
    ProjectionPath Path { get; }

    /// <summary>
    /// Gets the fully qualified <see cref="PropertyPath"/> that represents the array that children will be operated on. Only applies to child projections.
    /// </summary>
    PropertyPath ChildrenPropertyPath { get; }

    /// <summary>
    /// Gets whether or not there is a parent.
    /// </summary>
    bool HasParent { get; }

    /// <summary>
    /// Gets the parent projection - if any.
    /// </summary>
    IProjection? Parent { get; }

    /// <summary>
    /// Gets the <see cref="Model"/> the projection targets.
    /// </summary>
    Model Model { get; }

    /// <summary>
    /// Gets whether or not the projection is rewindable.
    /// </summary>
    bool IsRewindable { get; }

    /// <summary>
    /// Gets the <see cref="IObservable{T}">observable</see> <see cref="ProjectionEventContext">event</see>.
    /// </summary>
    IObservable<ProjectionEventContext> Event { get; }

    /// <summary>
    /// Gets the <see cref="IDictionary{TKey,TValue}"/> of <see cref="EventType"/> to <see cref="ProjectionOperationType"/> mapping. 
    /// </summary>
    IDictionary<EventType, ProjectionOperationType> OperationTypes { get; }

    /// <summary>
    /// Gets the <see cref="EventType">event types</see> the projection can handle.
    /// </summary>
    IEnumerable<EventType> EventTypes { get; }

    /// <summary>
    /// Gets the <see cref="EventType">event types</see> that are exclusive to this projection and not including any of the child projections.
    /// </summary>
    IEnumerable<EventType> OwnEventTypes { get; }

    /// <summary>
    /// Gets the <see cref="EventTypeWithKeyResolver"/> collection.
    /// </summary>
    IEnumerable<EventTypeWithKeyResolver> EventTypesWithKeyResolver { get; }

    /// <summary>
    /// Gets the collection of <see cref="IProjection">child projections</see>.
    /// </summary>
    IEnumerable<IProjection> ChildProjections { get; }

    /// <summary>
    /// Apply a filter to an <see cref="IObservable{EventContext}"/> with the event types the <see cref="Projection"/> is interested in.
    /// </summary>
    /// <param name="observable"><see cref="IObservable{EventContext}"/> to filter.</param>
    /// <returns>Filtered <see cref="IObservable{EventContext}"/>.</returns>
    IObservable<ProjectionEventContext> FilterEventTypes(IObservable<ProjectionEventContext> observable);

    /// <summary>
    /// Apply a filter to an <see cref="IObservable{Event}"/> with the event types the <see cref="Projection"/> is interested in.
    /// </summary>
    /// <param name="observable"><see cref="IObservable{Event}"/> to filter.</param>
    /// <returns>Filtered <see cref="IObservable{Event}"/>.</returns>
    IObservable<AppendedEvent> FilterEventTypes(IObservable<AppendedEvent> observable);

    /// <summary>
    /// Provides the projection with a new <see cref="AppendedEvent"/>.
    /// </summary>
    /// <param name="context"><see cref="ProjectionEventContext"/> to work with.</param>
    void OnNext(ProjectionEventContext context);

    /// <summary>
    /// Checks whether the projection will accept a specific event type.
    /// </summary>
    /// <param name="eventType"><see cref="EventType"/> to check.</param>
    /// <returns>True if it does, false if not.</returns>
    bool Accepts(EventType eventType);

    /// <summary>
    /// Get whether there is a key resolver for a specific <see cref="EventType"/>.
    /// </summary>
    /// <param name="eventType"><see cref="EventType"/> to check.</param>
    /// <returns>True if there is, false if not.</returns>
    bool HasKeyResolverFor(EventType eventType);

    /// <summary>
    /// Get the <see cref="ValueProvider{Event}"/> associated with a given <see cref="EventType"/>.
    /// </summary>
    /// <param name="eventType"><see cref="EventType"/> to get for.</param>
    /// <returns>The <see cref="KeyResolver"/>.</returns>
    KeyResolver GetKeyResolverFor(EventType eventType);

    /// <summary>
    /// Set event types with key resolvers for the projection.
    /// </summary>
    /// <param name="eventTypesWithKeyResolver">Collection of <see cref="EventTypeWithKeyResolver"/>.</param>
    /// <param name="ownEventTypes">Collection of <see cref="EventType"/> that is only for this projection without not any children.</param>
    /// <param name="operationTypes">Dictionary mapping <see cref="EventType"/> to <see cref="ProjectionOperationType"/>.</param>
    void SetEventTypesWithKeyResolvers(
        IEnumerable<EventTypeWithKeyResolver> eventTypesWithKeyResolver,
        IEnumerable<EventType> ownEventTypes,
        IDictionary<EventType, ProjectionOperationType> operationTypes);

    /// <summary>
    /// Set the parent <see cref="IProjection"/>.
    /// </summary>
    /// <param name="projection">The parent <see cref="IProjection"/>.</param>
    void SetParent(IProjection projection);
}
