// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Shares immutable discovery results that depend only on CLR types, not registry identities.
/// </summary>
/// <typeparam name="TValue">The immutable discovery result.</typeparam>
/// <remarks>
/// Entries never keep a type alive: results hang off the artifact through a <see cref="ConditionalWeakTable{TKey, TValue}"/>
/// and event type sets are held weakly, so collectible assemblies and test hosts can unload.
/// </remarks>
internal sealed class EventTypeSetCache<TValue>
    where TValue : class
{
    readonly ConditionalWeakTable<Type, ArtifactEntries> _artifacts = new();

    /// <summary>
    /// Gets the result for an artifact, model and unordered event type set.
    /// </summary>
    /// <param name="types">The registered CLR types.</param>
    /// <param name="artifact">The artifact being discovered.</param>
    /// <param name="model">The artifact's model.</param>
    /// <param name="build">The discovery operation.</param>
    /// <returns>The shared result.</returns>
    internal TValue Get(IEnumerable<Type> types, Type artifact, Type model, Func<IEnumerable<Type>, TValue> build) =>
        _artifacts.GetValue(artifact, static _ => new()).Get(model, [.. types], build);

    sealed class ArtifactEntries
    {
        readonly ConcurrentDictionary<TypeSetKey, Lazy<TValue>> _entries = new(TypeSetKey.Comparer);

        public TValue Get(Type model, HashSet<Type> types, Func<IEnumerable<Type>, TValue> build)
        {
            var probe = TypeSetKey.Probe(model, types);
            if (!_entries.TryGetValue(probe, out var entry))
            {
                RemoveCollected();
                entry = _entries.GetOrAdd(TypeSetKey.Weak(model, types), static (_, state) => new Lazy<TValue>(() => state.Build(state.Types)), (Build: build, Types: types));
            }

            try
            {
                return entry.Value;
            }
            catch
            {
                RemoveFailed(entry);
                throw;
            }
        }

        void RemoveFailed(Lazy<TValue> failed)
        {
            foreach (var pair in _entries.Where(pair => ReferenceEquals(pair.Value, failed)))
            {
                _entries.TryRemove(pair);
            }
        }

        void RemoveCollected()
        {
            foreach (var pair in _entries.Where(pair => pair.Key.IsCollected))
            {
                _entries.TryRemove(pair);
            }
        }
    }

    /// <summary>
    /// An unordered type set compared by membership; stored keys hold their types weakly, lookups probe with a set.
    /// </summary>
    sealed class TypeSetKey
    {
        readonly Type _model;
        readonly int _hash;
        readonly HashSet<Type>? _probe;
        readonly WeakReference<Type>[] _weak = [];

        TypeSetKey(Type model, HashSet<Type> types, bool weak)
        {
            _model = model;
            _hash = HashCode.Combine(model, types.Count, types.Aggregate(0, (hash, type) => hash ^ type.GetHashCode()));
            if (weak)
            {
                _weak = [.. types.Select(type => new WeakReference<Type>(type))];
            }
            else
            {
                _probe = types;
            }
        }

        public static IEqualityComparer<TypeSetKey> Comparer { get; } = new KeyComparer();

        public bool IsCollected => _weak.Any(reference => !reference.TryGetTarget(out _));

        int Count => _probe?.Count ?? _weak.Length;

        public static TypeSetKey Probe(Type model, HashSet<Type> types) => new(model, types, weak: false);

        public static TypeSetKey Weak(Type model, HashSet<Type> types) => new(model, types, weak: true);

        bool Contains(Type type)
        {
            if (_probe is not null) return _probe.Contains(type);
            foreach (var reference in _weak)
            {
                if (reference.TryGetTarget(out var target) && target == type) return true;
            }

            return false;
        }

        bool HasSameMembersAs(TypeSetKey other)
        {
            if (_model != other._model || Count != other.Count) return false;
            if (_probe is not null) return other._probe is not null ? _probe.SetEquals(other._probe) : other.HasSameMembersAs(this);
            foreach (var reference in _weak)
            {
                if (!reference.TryGetTarget(out var type) || !other.Contains(type)) return false;
            }

            return true;
        }

        sealed class KeyComparer : IEqualityComparer<TypeSetKey>
        {
            public bool Equals(TypeSetKey? x, TypeSetKey? y) =>
                ReferenceEquals(x, y) || (x is not null && y is not null && x.HasSameMembersAs(y));

            public int GetHashCode(TypeSetKey obj) => obj._hash;
        }
    }
}
