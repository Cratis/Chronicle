// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Caches routing against immutable registered descriptors, never against scenario state or CLR types alone.
/// </summary>
internal static class ObserverSequenceCache
{
    static readonly ConditionalWeakTable<FrozenDictionary<Type, EventTypeDescriptor>, Catalog> _catalogs = new();

    /// <summary>
    /// Infers a sequence from the registry's immutable catalog.
    /// </summary>
    /// <param name="observer">The observer.</param>
    /// <param name="registry">The owning registry.</param>
    /// <param name="store">The current store.</param>
    /// <param name="handled">The accepted identities, including generations and tombstones.</param>
    /// <returns>The inferred sequence.</returns>
    internal static EventSequenceId Infer(Type observer, EventTypes registry, EventStoreName store, IEnumerable<EventType> handled) =>
        _catalogs.GetValue(registry.Descriptors, _ => new Catalog(registry)).Infer(observer, store, handled);

    sealed class Catalog(EventTypes registry)
    {
        readonly (EventType Type, string? Store)[] _stores = registry.RegisteredTypes
            .Select(pair => (pair.Key, registry.GetDescriptorFor(pair.Value).EventStore)).ToArray();
        readonly FrozenSet<string> _declaredStores = registry.Descriptors.Values.Select(descriptor => descriptor.EventStore).OfType<string>().ToFrozenSet(StringComparer.Ordinal);
        readonly Dictionary<(Type Observer, string? Store), Entry> _sequences = [];
        readonly object _lock = new();

        internal EventSequenceId Infer(Type observer, EventStoreName store, IEnumerable<EventType> handled)
        {
            var types = handled.ToFrozenSet();
            lock (_lock)
            {
                // Undeclared store names have identical routing and must not grow the cache per scenario.
                var key = (observer, _declaredStores.Contains(store.Value) ? store.Value : null);
                if (_sequences.TryGetValue(key, out var entry) && entry.Handled.SetEquals(types))
                {
                    return entry.Sequence;
                }

                var stores = _stores.Where(item => types.Contains(item.Type)).Select(item => item.Store).OfType<string>().Distinct().ToArray();
                if (stores.Length > 1)
                {
                    throw new MultipleEventStoresDefined(observer, stores);
                }

                var sequence = stores.Length == 1 && stores[0] != store.Value
                    ? new EventSequenceId($"{EventSequenceId.InboxPrefix}{stores[0]}")
                    : EventSequenceId.Log;
                _sequences[key] = new(types, sequence);
                return sequence;
            }
        }
    }

    sealed record Entry(FrozenSet<EventType> Handled, EventSequenceId Sequence);
}
