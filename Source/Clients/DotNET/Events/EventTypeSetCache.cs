// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Frozen;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Shares immutable discovery results that depend only on CLR types, not registry identities.
/// </summary>
/// <typeparam name="TValue">The immutable discovery result.</typeparam>
internal sealed class EventTypeSetCache<TValue>
    where TValue : class
{
    readonly ConcurrentDictionary<CacheKey, Lazy<TValue>> _entries = new();

    /// <summary>
    /// Gets the result for an artifact, model and unordered event type set.
    /// </summary>
    /// <param name="types">The registered CLR types.</param>
    /// <param name="artifact">The artifact being discovered.</param>
    /// <param name="model">The artifact's model.</param>
    /// <param name="build">The discovery operation.</param>
    /// <returns>The shared result.</returns>
    internal TValue Get(IEnumerable<Type> types, Type artifact, Type model, Func<IEnumerable<Type>, TValue> build)
    {
        var snapshot = types.ToArray();
        var key = new CacheKey(snapshot.ToFrozenSet(), artifact, model);
        var entry = _entries.GetOrAdd(key, static (_, state) => new Lazy<TValue>(() => state.Build(state.Types)), (Build: build, Types: snapshot));
        try
        {
            return entry.Value;
        }
        catch
        {
            _entries.TryRemove(new KeyValuePair<CacheKey, Lazy<TValue>>(key, entry));
            throw;
        }
    }

    sealed class CacheKey(FrozenSet<Type> types, Type artifact, Type model) : IEquatable<CacheKey>
    {
        readonly FrozenSet<Type> _types = types;
        readonly Type _artifact = artifact;
        readonly Type _model = model;
        readonly int _hash = HashCode.Combine(artifact, model, types.Count, types.Aggregate(0, (hash, type) => hash ^ type.GetHashCode()));

        public bool Equals(CacheKey? other) => other is not null && _artifact == other._artifact && _model == other._model && _types.SetEquals(other._types);

        public override bool Equals(object? obj) => obj is CacheKey other && Equals(other);

        public override int GetHashCode() => _hash;
    }
}
