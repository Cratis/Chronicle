// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Remembers, per reducer and partition, the read model state the client last returned to the kernel.
/// </summary>
/// <remarks>
/// The kernel can answer a delivery with "initial state unchanged" instead of sending the state, meaning the state is the
/// one returned on the previous reduce of the partition. The state is kept as the JSON that was sent to the kernel, so
/// what a reducer later does with the object it was given can never change what the kernel has fingerprinted.
/// The cache is bounded and larger than the kernel's record of what it believes is cached. When full an arbitrary entry
/// is evicted; a later "unchanged" for it is answered with a failure, which makes the kernel redeliver the full state.
/// </remarks>
/// <param name="maxEntries">The most partitions to hold state for.</param>
internal sealed class ReducerStateCache(int maxEntries)
{
    /// <summary>
    /// The default for the most partitions held. Larger than what the kernel remembers per stream.
    /// </summary>
    internal const int DefaultMaxEntries = 100_000;

    readonly ConcurrentDictionary<(ReducerId Reducer, string Partition), string> _states = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ReducerStateCache"/> class with the default bound.
    /// </summary>
    internal ReducerStateCache()
        : this(DefaultMaxEntries)
    {
    }

    /// <summary>
    /// Gets the number of entries held.
    /// </summary>
    internal int Count => _states.Count;

    /// <summary>
    /// Remember the state returned for a partition. A null state forgets the partition.
    /// </summary>
    /// <param name="reducer">The <see cref="ReducerId"/>.</param>
    /// <param name="partition">The partition.</param>
    /// <param name="state">The JSON of the returned state, or null if the reducer returned no state.</param>
    internal void Set(ReducerId reducer, string partition, string? state)
    {
        if (state is null)
        {
            Forget(reducer, partition);
            return;
        }

        var key = (reducer, partition);
        if (!_states.ContainsKey(key) && _states.Count >= maxEntries)
        {
            foreach (var existing in _states.Keys)
            {
                _states.TryRemove(existing, out _);
                break;
            }
        }

        _states[key] = state;
    }

    /// <summary>
    /// Try to get the state returned for a partition on its previous reduce.
    /// </summary>
    /// <param name="reducer">The <see cref="ReducerId"/>.</param>
    /// <param name="partition">The partition.</param>
    /// <param name="state">The JSON of the state, when there is one.</param>
    /// <returns>True if there is a cached state.</returns>
    internal bool TryGet(ReducerId reducer, string partition, out string? state)
    {
        var found = _states.TryGetValue((reducer, partition), out var value);
        state = value;
        return found;
    }

    /// <summary>
    /// Forget the state of a partition.
    /// </summary>
    /// <param name="reducer">The <see cref="ReducerId"/>.</param>
    /// <param name="partition">The partition.</param>
    internal void Forget(ReducerId reducer, string partition) => _states.TryRemove((reducer, partition), out _);

    /// <summary>
    /// Forget the state of every partition of a reducer.
    /// </summary>
    /// <param name="reducer">The <see cref="ReducerId"/>.</param>
    internal void ForgetAll(ReducerId reducer)
    {
        foreach (var key in _states.Keys.Where(_ => _.Reducer == reducer))
        {
            _states.TryRemove(key, out _);
        }
    }
}
