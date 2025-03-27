// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents the context of an event when being handled by a <see cref="IProjection"/>.
/// </summary>
/// <param name="Key"><see cref="Key"/> for the context.</param>
/// <param name="Event">The <see cref="AppendedEvent"/> that occurred.</param>
/// <param name="Changeset">The <see cref="IChangeset{Event, ExpandoObject}"/> to build on.</param>
/// <param name="OperationType"><see cref="ProjectionOperationType"/>.</param>
/// <param name="NeedsInitialState">Whether the projection needs initial state.</param>
public record ProjectionEventContext(
    Key Key,
    AppendedEvent Event,
    IChangeset<AppendedEvent, ExpandoObject> Changeset,
    ProjectionOperationType OperationType,
    bool NeedsInitialState)
{
    /// <summary>
    /// Gets the <see cref="EventType"/> of the <see cref="Event"/>.
    /// </summary>
    public EventType EventType => Event.Metadata.Type;

    /// <summary>
    /// Gets the <see cref="EventSequenceNumber"/> of the <see cref="Event"/>.
    /// </summary>
    public EventSequenceNumber EventSequenceNumber => Event.Metadata.SequenceNumber;

    /// <summary>
    /// Whether the operation type is a join.
    /// </summary>
    public bool IsJoin => OperationType.HasFlag(ProjectionOperationType.Join);

    /// <summary>
    /// Whether the operation type is a remove.
    /// </summary>
    public bool IsRemove => OperationType.HasFlag(ProjectionOperationType.Remove);

    /// <summary>
    /// Whether the operation type affects children.
    /// </summary>
    public bool ChildrenAffected => OperationType.HasFlag(ProjectionOperationType.ChildrenAffected);

    /// <summary>
    /// Creates a new empty <see cref="ProjectionEventContext"/> with the given <see cref="IObjectComparer"/> and
    /// <see cref="AppendedEvent"/>.
    /// </summary>
    /// <param name="comparer">The <see cref="IObjectComparer"/>.</param>
    /// <param name="event">The <see cref="AppendedEvent"/>.</param>
    /// <returns>The <see cref="ProjectionEventContext"/>.</returns>
    public static ProjectionEventContext Empty(IObjectComparer comparer, AppendedEvent @event) => new(
        Key.Undefined,
        @event,
        new Changeset<AppendedEvent, ExpandoObject>(comparer, @event, new ExpandoObject()),
        ProjectionOperationType.None,
        false);
}
