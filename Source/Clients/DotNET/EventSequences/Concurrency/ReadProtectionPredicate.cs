// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Normalizes and compares predicates without changing legacy scope semantics.
/// </summary>
internal static class ReadProtectionPredicate
{
    /// <summary>
    /// Snapshots type IDs and removes wildcard constraints.
    /// </summary>
    /// <param name="scope">The captured condition.</param>
    /// <returns>The normalized immutable condition.</returns>
    internal static ConcurrencyScope Normalize(ConcurrencyScope scope) => scope with
    {
        EventSourceId = scope.EventSourceId is null || string.IsNullOrEmpty(scope.EventSourceId.Value) || scope.EventSourceId.Value == "*" ? null : new(scope.EventSourceId.Value),
        EventSourceType = scope.EventSourceType is null || string.IsNullOrEmpty(scope.EventSourceType.Value) || scope.EventSourceType == EventSourceType.Default || scope.EventSourceType == EventSourceType.Unspecified ? null : scope.EventSourceType,
        EventStreamType = scope.EventStreamType is null || string.IsNullOrEmpty(scope.EventStreamType.Value) || scope.EventStreamType.IsAll ? null : scope.EventStreamType,
        EventStreamId = scope.EventStreamId is null || string.IsNullOrEmpty(scope.EventStreamId.Value) || scope.EventStreamId.IsDefault ? null : scope.EventStreamId,
        EventTypes = (scope.EventTypes ?? []).DistinctBy(_ => _.Id).ToImmutableArray()
    };

    /// <summary>
    /// Compares normalized predicates and their boundaries by meaning.
    /// </summary>
    /// <param name="first">The first condition.</param>
    /// <param name="second">The second condition.</param>
    /// <returns>Whether both conditions have the same meaning.</returns>
    internal static bool Same(ConcurrencyScope first, ConcurrencyScope second) =>
        first.SequenceNumber == second.SequenceNumber &&
        first.ExpectsNoMatchingEvent == second.ExpectsNoMatchingEvent &&
        Covers(first, second) && Covers(second, first);

    /// <summary>
    /// Checks predicate containment without inferring boundary coverage.
    /// </summary>
    /// <param name="outer">The enclosing predicate.</param>
    /// <param name="inner">The required predicate.</param>
    /// <returns>Whether the outer predicate includes every inner match.</returns>
    internal static bool Covers(ConcurrencyScope outer, ConcurrencyScope inner) =>
        (outer.EventSourceId is null || outer.EventSourceId.Value == inner.EventSourceId?.Value) &&
        (outer.EventSourceType is null || outer.EventSourceType == inner.EventSourceType) &&
        (outer.EventStreamType is null || outer.EventStreamType == inner.EventStreamType) &&
        (outer.EventStreamId is null || outer.EventStreamId == inner.EventStreamId) && CoversTypes(outer, inner);

    /// <summary>
    /// Checks that the enclosing condition retains an equally early boundary.
    /// </summary>
    /// <param name="outer">The enclosing condition.</param>
    /// <param name="inner">The required condition.</param>
    /// <returns>Whether the outer boundary covers the inner boundary.</returns>
    internal static bool CoversBoundary(ConcurrencyScope outer, ConcurrencyScope inner) =>
        outer.ExpectsNoMatchingEvent ||
        (outer.SequenceNumber.IsActualValue && inner.SequenceNumber.IsActualValue && outer.SequenceNumber.Value <= inner.SequenceNumber.Value);

    /// <summary>
    /// Checks a normalized predicate against a staged event.
    /// </summary>
    /// <param name="scope">The acquired dependency.</param>
    /// <param name="event">The staged event.</param>
    /// <param name="type">The staged event's registered type.</param>
    /// <returns>Whether the staged event overlaps the dependency.</returns>
    internal static bool Matches(ConcurrencyScope scope, EventForEventSourceId @event, EventType type) =>
        (scope.EventSourceId is null || scope.EventSourceId.Value == @event.EventSourceId.Value) &&
        (scope.EventSourceType is null || scope.EventSourceType == @event.EventSourceType) &&
        (scope.EventStreamType is null || scope.EventStreamType == @event.EventStreamType) &&
        (scope.EventStreamId is null || scope.EventStreamId == @event.EventStreamId) &&
        (!scope.EventTypes!.Any() || scope.EventTypes!.Any(_ => _.Id == type.Id));

    /// <summary>
    /// Forms a conservative predicate superset while retaining the first boundary.
    /// </summary>
    /// <param name="first">The first normalized predicate.</param>
    /// <param name="second">The second normalized predicate.</param>
    /// <returns>The predicate union with only common equalities.</returns>
    internal static ConcurrencyScope Merge(ConcurrencyScope first, ConcurrencyScope second) => first with
    {
        EventSourceId = first.EventSourceId?.Value == second.EventSourceId?.Value ? first.EventSourceId : null,
        EventSourceType = first.EventSourceType == second.EventSourceType ? first.EventSourceType : null,
        EventStreamType = first.EventStreamType == second.EventStreamType ? first.EventStreamType : null,
        EventStreamId = first.EventStreamId == second.EventStreamId ? first.EventStreamId : null,
        EventTypes = !first.EventTypes!.Any() || !second.EventTypes!.Any() ? ImmutableArray<EventType>.Empty : first.EventTypes!.Concat(second.EventTypes!).DistinctBy(_ => _.Id).ToImmutableArray()
    };

    /// <summary>
    /// Retains the earliest global boundary, including the negative-read sentinel.
    /// </summary>
    /// <param name="first">The first global boundary.</param>
    /// <param name="second">The second global boundary.</param>
    /// <returns>The earliest boundary.</returns>
    internal static EventSequenceNumber Earliest(EventSequenceNumber first, EventSequenceNumber second)
    {
        if (first.IsBeforeFirst || second.IsBeforeFirst) return EventSequenceNumber.BeforeFirst;
        return first.Value <= second.Value ? first : second;
    }

    static bool CoversTypes(ConcurrencyScope outer, ConcurrencyScope inner)
    {
        var outerTypes = outer.EventTypes!.Select(_ => _.Id).ToHashSet();
        var innerTypes = inner.EventTypes!.Select(_ => _.Id).ToHashSet();
        return outerTypes.Count == 0 || (innerTypes.Count != 0 && outerTypes.IsSupersetOf(innerTypes));
    }
}
