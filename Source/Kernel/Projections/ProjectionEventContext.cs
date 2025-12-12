// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;

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
    readonly List<ProjectionFuture> _deferredFutures = [];

    /// <summary>
    /// Gets the collection of deferred futures that need to be stored.
    /// </summary>
    public IEnumerable<ProjectionFuture> DeferredFutures => _deferredFutures;

    /// <summary>
    /// Gets whether this event has been deferred (has futures that couldn't be resolved).
    /// </summary>
    public bool IsDeferred => _deferredFutures.Count > 0;

    /// <summary>
    /// Gets the <see cref="EventType"/> of the <see cref="Event"/>.
    /// </summary>
    public EventType EventType => Event.Context.EventType;

    /// <summary>
    /// Gets the <see cref="EventSequenceNumber"/> of the <see cref="Event"/>.
    /// </summary>
    public EventSequenceNumber EventSequenceNumber => Event.Context.SequenceNumber;

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
    /// Adds a deferred future to the context.
    /// </summary>
    /// <param name="future">The <see cref="ProjectionFuture"/> to add.</param>
    public void AddDeferredFuture(ProjectionFuture future)
    {
        // Avoid adding duplicate futures for the same event sequence number
        if (_deferredFutures.Exists(f => f.Event.Context.SequenceNumber == future.Event.Context.SequenceNumber))
        {
            return;
        }

        _deferredFutures.Add(future);
    }

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
