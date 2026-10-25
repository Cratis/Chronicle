// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

internal sealed partial class ReplayScopedEventSequenceStorage
{
    readonly ConcurrentDictionary<string, ParentHistoryWindow> _parentHistory = new();

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>>> GetInstancesOfAnyInRange(
        IEnumerable<EventSourceId> eventSourceIds,
        IEnumerable<EventTypeId> eventTypes,
        EventSequenceNumber from,
        EventSequenceNumber upTo,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sources = eventSourceIds.Distinct().ToArray();
        var types = eventTypes as EventTypeId[] ?? eventTypes.ToArray();
        if (_active && TryGetPreparedHistory(sources, types, from, upTo, out var cached)) return cached;

        var results = await inner.GetInstancesOfAnyInRange(sources, types, from, upTo, cancellationToken);
        if (_active && _preparingPage && from <= upTo)
        {
            foreach (var source in sources)
            {
                var key = BuildKey(source, types.Select(type => type.Value));
                _parentHistory.TryAdd(key, new(from, upTo, results.GetValueOrDefault(source) ?? []));
            }
        }
        return results;
    }

    bool TryGetPreparedHistory(
        EventSourceId[] sources,
        EventTypeId[] types,
        EventSequenceNumber from,
        EventSequenceNumber upTo,
        out IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> results)
    {
        var found = new Dictionary<EventSourceId, IReadOnlyList<AppendedEvent>>();
        results = found;
        foreach (var source in sources)
        {
            var key = BuildKey(source, types.Select(type => type.Value));
            if (!_parentHistory.TryGetValue(key, out var window) || !window.Covers(from, upTo)) return false;
            var events = window.Select(from, upTo);
            if (events.Count > 0) found[source] = events;
        }
        return true;
    }
}
