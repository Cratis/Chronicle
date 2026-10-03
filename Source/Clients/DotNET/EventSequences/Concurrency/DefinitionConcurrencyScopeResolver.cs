// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Resolves the concurrency scope of one event source id in a batch appended through event source definitions.
/// </summary>
/// <remarks>
/// <para>
/// The append protocol carries one scope per event source id, so every guard the batch needs for that id has to be
/// the same guard. Every entry is resolved through the strategy, and the guards that actually check something are
/// compared by their predicate: source id, source type, stream type, stream id and the set of event types. The
/// expected sequence number is not part of the predicate, because it is read at a point in time and differs
/// between reads of the same predicate; the first guard that checks something is kept, which is the conservative one.
/// </para>
/// <para>
/// An entry whose strategy result checks nothing (<see cref="ConcurrencyScope.None"/>, an unresolved scope, or one
/// that is incomplete and so validated against nothing) needs no guard and never hides a later entry that does.
/// Two entries needing different guards cannot be represented, so <see cref="IncompatibleConcurrencyScopesForEventSource"/>
/// is thrown rather than dropping one of them.
/// </para>
/// </remarks>
internal static class DefinitionConcurrencyScopeResolver
{
    /// <summary>
    /// Resolve the scope for all the entries sharing an event source id.
    /// </summary>
    /// <param name="eventSourceId">The shared <see cref="EventSourceId"/>.</param>
    /// <param name="entries">The events with their routing, in batch order.</param>
    /// <param name="strategy">The <see cref="IConcurrencyScopeStrategy"/> to resolve each entry with.</param>
    /// <returns>The <see cref="ConcurrencyScope"/> to send for the event source id.</returns>
    /// <exception cref="IncompatibleConcurrencyScopesForEventSource">The entries require different guards.</exception>
    public static async Task<ConcurrencyScope> Resolve(
        EventSourceId eventSourceId,
        IReadOnlyList<(EventForEventSourceId Event, ResolvedEventRouting? Routing)> entries,
        IConcurrencyScopeStrategy strategy)
    {
        // Legacy-only batches keep the long-standing behavior of using the first event.
        var relevant = entries.Any(_ => _.Routing is not null) ? entries : [entries[0]];
        var resolved = new List<ConcurrencyScope>();
        var cache = new Dictionary<object, ConcurrencyScope>();

        foreach (var (@event, routing) in relevant)
        {
            var key = (routing?.Dimensions, @event.EventStreamType, @event.EventStreamId, @event.EventSourceType);
            if (!cache.TryGetValue(key, out var scope))
            {
                scope = routing is null
                    ? await strategy.GetScope(eventSourceId, @event.EventStreamType, @event.EventStreamId, @event.EventSourceType)
                    : await strategy.GetScope(routing.Dimensions, eventSourceId, @event.EventStreamType, @event.EventStreamId, @event.EventSourceType);
                cache[key] = scope;
            }

            resolved.Add(scope);
        }

        var guards = resolved.Where(ChecksSomething).ToList();
        if (guards.Count == 0)
        {
            return resolved.FirstOrDefault(_ => _ != ConcurrencyScope.NotSet) ?? resolved[0];
        }

        var distinct = new List<ConcurrencyScope>();
        guards.ForEach(guard =>
        {
            if (!distinct.Exists(_ => SamePredicate(_, guard)))
            {
                distinct.Add(guard);
            }
        });

        return distinct.Count > 1
            ? throw new IncompatibleConcurrencyScopesForEventSource(eventSourceId, distinct)
            : guards[0];
    }

    static bool ChecksSomething(ConcurrencyScope scope) =>
        scope != ConcurrencyScope.NotSet && scope != ConcurrencyScope.None && (scope.SequenceNumber.IsActualValue || scope.SequenceNumber.IsBeforeFirst);

    static bool SamePredicate(ConcurrencyScope left, ConcurrencyScope right) =>
        left.EventSourceId == right.EventSourceId &&
        left.EventSourceType == right.EventSourceType &&
        left.EventStreamType == right.EventStreamType &&
        left.EventStreamId == right.EventStreamId &&
        (left.EventTypes ?? []).ToHashSet().SetEquals(right.EventTypes ?? []);
}
