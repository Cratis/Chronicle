// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Provides the canonical form of the collections and scope of a constraint definition.
/// </summary>
/// <remarks>
/// The definition a client registers is rebuilt on every connect, from attributes discovered by reflection, so the
/// order its collections come in is not something the declaration says. Whether a registered definition differs from
/// the stored one, whether the difference requires a reindex and the version derived from the definitions all read the
/// same canonical form, so a definition that means the same thing never looks changed. The order of the properties
/// within one event definition is deliberately not touched: it is the order the claimed value is joined in, so it is
/// part of what the constraint means.
/// </remarks>
public static class ConstraintNormalization
{
    /// <summary>
    /// Get the canonical form of a set of event type ids: distinct and ordered by value.
    /// </summary>
    /// <param name="eventTypeIds">The event type ids.</param>
    /// <returns>The canonical event type ids.</returns>
    public static IReadOnlyList<EventTypeId> Canonical(this IEnumerable<EventTypeId> eventTypeIds) =>
        [.. eventTypeIds.Distinct().OrderBy(_ => _.Value, StringComparer.Ordinal)];

    /// <summary>
    /// Get the canonical form of a set of event sequence ids: distinct and ordered by value.
    /// </summary>
    /// <param name="eventSequences">The event sequence ids.</param>
    /// <returns>The canonical event sequence ids.</returns>
    public static IReadOnlyList<EventSequenceId> Canonical(this IEnumerable<EventSequenceId> eventSequences) =>
        [.. eventSequences.Distinct().OrderBy(_ => _.Value, StringComparer.Ordinal)];

    /// <summary>
    /// Get the canonical form of a set of event definitions: duplicates dropped, ordered by event type.
    /// </summary>
    /// <param name="eventDefinitions">The event definitions.</param>
    /// <returns>The canonical event definitions.</returns>
    /// <remarks>
    /// The ordering is stable, so several definitions for one event type keep the order they were declared in.
    /// </remarks>
    public static IReadOnlyList<UniqueConstraintEventDefinition> Canonical(this IEnumerable<UniqueConstraintEventDefinition> eventDefinitions) =>
        [.. eventDefinitions.Distinct().OrderBy(_ => _.EventTypeId.Value, StringComparer.Ordinal)];

    /// <summary>
    /// Get the canonical form of a scope: a scope that scopes nothing is no scope.
    /// </summary>
    /// <param name="scope">The scope, if any.</param>
    /// <returns>The scope, or <see langword="null"/> when it applies no scoping.</returns>
    public static ConstraintScope? Canonical(this ConstraintScope? scope) => scope is { HasScope: true } ? scope : null;

    /// <summary>
    /// Check whether two collections of event type ids hold the same set of ids, regardless of order and duplicates.
    /// </summary>
    /// <param name="eventTypeIds">The event type ids.</param>
    /// <param name="other">The other event type ids.</param>
    /// <returns>True if they hold the same ids, false if not.</returns>
    public static bool SameSetAs(this IEnumerable<EventTypeId> eventTypeIds, IEnumerable<EventTypeId> other) =>
        eventTypeIds.ToHashSet().SetEquals(other);
}
