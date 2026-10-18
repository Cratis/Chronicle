// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Resolves the concurrency scope of one event source id in a batch appended through event source definitions.
/// </summary>
/// <remarks>
/// Every distinct routing predicate is resolved. Equal predicates retain the earliest expected boundary;
/// unchecked entries never hide a later guard. The transport checks kernel conjunction support before sending.
/// </remarks>
internal static class DefinitionConcurrencyScopeResolver
{
    /// <summary>
    /// Resolve the scope for all the entries sharing an event source id.
    /// </summary>
    /// <param name="eventSourceId">The shared <see cref="EventSourceId"/>.</param>
    /// <param name="entries">The events with their routing, in batch order.</param>
    /// <param name="strategy">The <see cref="IConcurrencyScopeStrategy"/> to resolve each entry with.</param>
    /// <param name="honorDeclaredConcurrency">Whether Enforce metadata supplies deferred dimensions.</param>
    /// <returns>The scopes to send for the event source id.</returns>
    public static async Task<IReadOnlyList<ConcurrencyScope>> Resolve(
        EventSourceId eventSourceId,
        IReadOnlyList<(EventForEventSourceId Event, ResolvedEventRouting? Routing)> entries,
        IConcurrencyScopeStrategy strategy,
        bool honorDeclaredConcurrency = false)
    {
        var resolved = new List<ConcurrencyScope>();
        var cache = new Dictionary<object, ConcurrencyScope>();

        foreach (var (@event, routing) in entries)
        {
            var dimensions = honorDeclaredConcurrency ? @event.DeclaredConcurrency ?? routing?.Dimensions : routing?.Dimensions;
            var key = (dimensions, @event.EventStreamType, @event.EventStreamId, @event.EventSourceType);
            if (!cache.TryGetValue(key, out var scope))
            {
                scope = dimensions is null
                    ? await strategy.GetScope(eventSourceId, @event.EventStreamType, @event.EventStreamId, @event.EventSourceType)
                    : await strategy.GetScope(dimensions.Value, eventSourceId, @event.EventStreamType, @event.EventStreamId, @event.EventSourceType);
                scope = scope with { IssuedByStrategy = true };
                cache[key] = scope;
            }

            resolved.Add(scope);
        }

        var guards = resolved.Where(ChecksSomething).ToList();
        if (guards.Count == 0)
        {
            return [resolved.Find(_ => _ != ConcurrencyScope.NotSet) ?? resolved[0]];
        }

        var distinct = new List<ConcurrencyScope>();
        guards.ForEach(guard =>
        {
            var index = distinct.FindIndex(_ => SamePredicate(_, guard));
            if (index < 0)
            {
                distinct.Add(guard);
            }
            else
            {
                distinct[index] = distinct[index] with { SequenceNumber = ReadProtectionPredicate.Earliest(distinct[index].SequenceNumber, guard.SequenceNumber) };
            }
        });

        return distinct;
    }

    /// <summary>
    /// Compares the narrowing independently of its expected boundary.
    /// </summary>
    /// <param name="left">The first scope.</param>
    /// <param name="right">The second scope.</param>
    /// <returns>Whether both scopes guard the same predicate.</returns>
    internal static bool SamePredicate(ConcurrencyScope left, ConcurrencyScope right) =>
        left.EventSourceId?.Value == right.EventSourceId?.Value &&
        left.EventSourceType == right.EventSourceType &&
        left.EventStreamType == right.EventStreamType &&
        left.EventStreamId == right.EventStreamId &&
        (left.EventTypes ?? []).ToHashSet().SetEquals(right.EventTypes ?? []);

    static bool ChecksSomething(ConcurrencyScope scope) =>
        scope != ConcurrencyScope.NotSet && scope != ConcurrencyScope.None && (scope.SequenceNumber.IsActualValue || scope.SequenceNumber.IsBeforeFirst);
}
