// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Collects bounded parent reads by event-type set, retaining missing sources without repeating reads.
/// </summary>
/// <param name="storage">The replay-scoped event sequence.</param>
/// <param name="from">The first sequence in the page.</param>
/// <param name="upTo">The maximum sequence in the page.</param>
internal sealed class PageParentReads(IEventSequenceStorage storage, EventSequenceNumber from, EventSequenceNumber upTo)
{
    readonly Dictionary<string, Dictionary<EventSourceId, ParentHistoryWindow>> _loaded = new();
    readonly Dictionary<string, (EventTypeId[] Types, HashSet<EventSourceId> Sources)> _pending = new();

    /// <summary>
    /// Gets whether a new batch of parent reads is needed.
    /// </summary>
    internal bool HasPending => _pending.Count > 0;

    /// <summary>
    /// Finds a loaded parent as of the child or enrolls its source in the next batch.
    /// </summary>
    /// <param name="source">The parent event source.</param>
    /// <param name="types">The parent's own event types.</param>
    /// <returns>The latest eligible parent, when already loaded and present.</returns>
    internal AppendedEvent? Find(EventSourceId source, EventTypeId[] types)
    {
        var key = string.Join('|', types.Select(type => type.Value).Distinct().Order(StringComparer.Ordinal));
        if (_loaded.TryGetValue(key, out var loaded) && loaded.TryGetValue(source, out var result))
        {
            var bound = ParentKeyResolutionScope.Current;
            return result.Find(bound.SequenceNumber, bound.Inclusive);
        }

        if (!_pending.TryGetValue(key, out var request))
        {
            request = (types, []);
            _pending[key] = request;
        }
        request.Sources.Add(source);
        return null;
    }

    /// <summary>
    /// Loads the page window and latest predecessor for all enrolled sources. Omitted sources are misses.
    /// </summary>
    /// <returns>A task representing the batched reads.</returns>
    internal async Task Load()
    {
        foreach (var (key, request) in _pending)
        {
            var results = await storage.GetInstancesOfAnyInRange(request.Sources, request.Types, from, upTo);
            if (!_loaded.TryGetValue(key, out var loaded))
            {
                loaded = new();
                _loaded[key] = loaded;
            }
            foreach (var source in request.Sources) loaded[source] = new(from, upTo, results.GetValueOrDefault(source) ?? []);
        }
        _pending.Clear();
    }
}
