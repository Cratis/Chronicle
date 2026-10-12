// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Caches discovery per registry, type universe and reference-identified discovery context.
/// </summary>
/// <typeparam name="TValue">The cached discovery result.</typeparam>
internal sealed class EventTypeRegistryCache<TValue>
    where TValue : class
{
    readonly ConditionalWeakTable<IEventTypes, RegistryCache> _registries = new();

    /// <summary>
    /// Gets a discovery result for the current registry snapshot.
    /// </summary>
    /// <param name="registry">The event registry.</param>
    /// <param name="type">The artifact type.</param>
    /// <param name="context">An optional additional reference-identified discovery context.</param>
    /// <param name="build">The discovery operation.</param>
    /// <returns>The discovery result.</returns>
    internal TValue Get(IEventTypes registry, Type type, object? context, Func<IEnumerable<Type>, TValue> build) =>
        _registries.GetValue(registry, static _ => new()).Get(registry.AllClrTypes, type, context, build);

    sealed class RegistryCache
    {
        readonly object _lock = new();
        FrozenSet<Type> _types = FrozenSet<Type>.Empty;
        Dictionary<Type, TValue> _defaults = [];
        ConditionalWeakTable<object, Dictionary<Type, TValue>> _contexts = new();

        public TValue Get(IEnumerable<Type> types, Type type, object? context, Func<IEnumerable<Type>, TValue> build)
        {
            var snapshot = types.ToArray();
            lock (_lock)
            {
                if (!_types.SetEquals(snapshot))
                {
                    _types = snapshot.ToFrozenSet();
                    _defaults = [];
                    _contexts = new();
                }

                var entries = context is null ? _defaults : _contexts.GetValue(context, static _ => []);
                if (!entries.TryGetValue(type, out var value))
                {
                    value = build(snapshot);
                    entries.Add(type, value);
                }

                return value;
            }
        }
    }
}
