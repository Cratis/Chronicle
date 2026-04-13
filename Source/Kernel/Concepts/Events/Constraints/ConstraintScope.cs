// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Represents the scope of a constraint, defining optional boundaries for constraint validation.
/// </summary>
/// <param name="EventSourceType">Optional <see cref="Events.EventSourceType"/> to scope the constraint to.</param>
/// <param name="EventStreamType">Optional <see cref="Events.EventStreamType"/> to scope the constraint to.</param>
/// <param name="EventStreamId">Optional <see cref="Events.EventStreamId"/> to scope the constraint to.</param>
public record ConstraintScope(
    EventSourceType? EventSourceType = default,
    EventStreamType? EventStreamType = default,
    EventStreamId? EventStreamId = default)
{
    /// <summary>
    /// Represents a constraint scope with no scoping applied.
    /// </summary>
    public static readonly ConstraintScope None = new();

    /// <summary>
    /// Gets a value indicating whether this scope has any scoping applied.
    /// </summary>
    public bool HasScope => EventSourceType is not null || EventStreamType is not null || EventStreamId is not null;
}
