// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Extension methods for <see cref="ConstraintScope"/>.
/// </summary>
public static class ConstraintScopeExtensions
{
    /// <summary>
    /// Build a storage scope key from the constraint scope and the event metadata.
    /// </summary>
    /// <param name="scope">The <see cref="ConstraintScope"/> defining which dimensions to include.</param>
    /// <param name="eventSourceType">The <see cref="EventSourceType"/> of the event.</param>
    /// <param name="eventStreamType">The <see cref="EventStreamType"/> of the event.</param>
    /// <param name="eventStreamId">The <see cref="EventStreamId"/> of the event.</param>
    /// <returns>A string key representing the scope dimensions, or empty if no scoping is applied.</returns>
    public static string BuildScopeKey(
        this ConstraintScope? scope,
        EventSourceType? eventSourceType,
        EventStreamType? eventStreamType,
        EventStreamId? eventStreamId)
    {
        if (scope?.HasScope != true)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        if (scope.EventSourceType is not null && eventSourceType is not null)
        {
            parts.Add($"est:{eventSourceType.Value}");
        }

        if (scope.EventStreamType is not null && eventStreamType is not null)
        {
            parts.Add($"estt:{eventStreamType.Value}");
        }

        if (scope.EventStreamId is not null && eventStreamId is not null)
        {
            parts.Add($"esid:{eventStreamId.Value}");
        }

        return string.Join('|', parts);
    }

    /// <summary>
    /// Resolve the scope declared on a constraint into the dimension values of the event being validated.
    /// </summary>
    /// <param name="scope">The <see cref="ConstraintScope"/> declaring which dimensions participate.</param>
    /// <param name="eventSourceType">The <see cref="EventSourceType"/> of the event.</param>
    /// <param name="eventStreamType">The <see cref="EventStreamType"/> of the event.</param>
    /// <param name="eventStreamId">The <see cref="EventStreamId"/> of the event.</param>
    /// <returns>
    /// A <see cref="ResolvedConstraintScope"/> carrying the event's own value for every participating dimension,
    /// or <see langword="null"/> when the constraint is not scoped and nothing should be narrowed.
    /// </returns>
    /// <remarks>
    /// A dimension participates only when the declaration names it and the event actually carries a value for it -
    /// the same rule <see cref="BuildScopeKey"/> applies, so a lookup narrowed by the resolved scope and a key built
    /// from the same event always agree on which dimensions matter.
    /// </remarks>
    public static ResolvedConstraintScope? ResolveFor(
        this ConstraintScope? scope,
        EventSourceType? eventSourceType,
        EventStreamType? eventStreamType,
        EventStreamId? eventStreamId)
    {
        if (scope?.HasScope != true)
        {
            return null;
        }

        return new(
            scope.EventSourceType is not null ? eventSourceType : null,
            scope.EventStreamType is not null ? eventStreamType : null,
            scope.EventStreamId is not null ? eventStreamId : null);
    }
}
