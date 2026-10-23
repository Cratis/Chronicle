// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Collects parent reads by event-type set without substituting missing or partial batch results for real reads.
/// </summary>
/// <param name="storage">The replay-scoped event sequence.</param>
internal sealed class PageParentReads(IEventSequenceStorage storage)
{
    readonly Dictionary<string, Dictionary<EventSourceId, Option<AppendedEvent>>> _loaded = new();
    readonly Dictionary<string, (EventTypeId[] Types, HashSet<EventSourceId> Sources)> _pending = new();

    /// <summary>Gets whether a new batch of parent reads is needed.</summary>
    internal bool HasPending => _pending.Count > 0;

    /// <summary>Finds a loaded parent or enrolls its source in the next batch.</summary>
    /// <param name="source">The parent event source.</param>
    /// <param name="types">The parent's own event types.</param>
    /// <returns>The latest matching parent, when already loaded and present.</returns>
    internal AppendedEvent? Find(EventSourceId source, EventTypeId[] types)
    {
        var key = string.Join('|', types.Select(type => type.Value).Distinct().Order(StringComparer.Ordinal));
        if (_loaded.TryGetValue(key, out var loaded) && loaded.TryGetValue(source, out var result))
        {
            return result.TryGetValue(out var found) ? found : null;
        }

        if (!_pending.TryGetValue(key, out var request))
        {
            request = (types, []);
            _pending[key] = request;
        }
        request.Sources.Add(source);
        return null;
    }

    /// <summary>Loads all enrolled sources, rejecting partial results.</summary>
    /// <returns>A task representing the batched reads.</returns>
    /// <exception cref="InvalidOperationException">A provider returned an incomplete batch.</exception>
    internal async Task Load()
    {
        foreach (var (key, request) in _pending)
        {
            var results = await storage.GetLastInstancesOfAny(request.Sources, request.Types);
            if (request.Sources.Any(source => !results.ContainsKey(source)))
            {
                throw new InvalidOperationException("A prepared parent batch did not include every requested source.");
            }

            if (!_loaded.TryGetValue(key, out var loaded))
            {
                loaded = new();
                _loaded[key] = loaded;
            }
            foreach (var source in request.Sources) loaded[source] = results[source];
        }
        _pending.Clear();
    }
}
