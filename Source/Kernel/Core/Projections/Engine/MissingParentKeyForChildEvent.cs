// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// The exception that is thrown when the configured parent key for a child projection resolves to no value
/// for an event — the event does not carry the property named as the parent key in the projection definition.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingParentKeyForChildEvent"/> class.
/// </remarks>
/// <param name="projection">The child <see cref="IProjection"/> the key was being resolved for.</param>
/// <param name="event">The <see cref="AppendedEvent"/> that does not carry a value for the parent key.</param>
public class MissingParentKeyForChildEvent(IProjection projection, AppendedEvent @event) : Exception(
    $"The parent key for child projection '{projection.Path}' (children at '{projection.ChildrenPropertyPath}', identified by '{projection.IdentifiedByProperty}') " +
    $"in projection '{projection.Identifier}' resolved to no value for event type '{@event.Context.EventType.Id}' at sequence number {@event.Context.SequenceNumber} " +
    $"on event source '{@event.Context.EventSourceId}'. The event does not carry the property configured as the parent key for this event type — " +
    "verify the 'parent' expression in the projection definition against the event's content.")
{
    /// <summary>
    /// Gets the identifier of the projection the key was being resolved for.
    /// </summary>
    public ProjectionId ProjectionIdentifier { get; } = projection.Identifier;

    /// <summary>
    /// Gets the path of the child projection within the projection hierarchy.
    /// </summary>
    public ProjectionPath ProjectionPath { get; } = projection.Path;

    /// <summary>
    /// Gets the property path of the children collection the child projection targets.
    /// </summary>
    public PropertyPath ChildrenPropertyPath { get; } = projection.ChildrenPropertyPath;

    /// <summary>
    /// Gets the identifier of the event type the parent key could not be resolved for.
    /// </summary>
    public EventTypeId EventTypeId { get; } = @event.Context.EventType.Id;

    /// <summary>
    /// Gets the sequence number of the event the parent key could not be resolved for.
    /// </summary>
    public EventSequenceNumber SequenceNumber { get; } = @event.Context.SequenceNumber;
}
