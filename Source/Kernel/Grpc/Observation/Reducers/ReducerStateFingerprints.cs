// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.Services.Observation.Reducers;

/// <summary>
/// Remembers, per partition, the fingerprint of the read model state a client returned on its previous reduce.
/// </summary>
/// <remarks>
/// The cache belongs to one observation stream and is never shared. It is bounded: when full, an arbitrary entry is
/// evicted, which only makes the next delivery for that partition carry the full state.
/// </remarks>
/// <param name="maxEntries">The most partitions to remember.</param>
internal sealed class ReducerStateFingerprints(int maxEntries)
{
    readonly ConcurrentDictionary<string, ulong> _fingerprints = new();
    volatile bool _enabled;

    /// <summary>
    /// Gets or sets a value indicating whether the client of the stream opted in to state caching.
    /// </summary>
    internal bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    /// <summary>
    /// Gets the number of partitions remembered.
    /// </summary>
    internal int Count => _fingerprints.Count;

    /// <summary>
    /// Remember the fingerprint of the state returned for a partition.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <param name="fingerprint">The fingerprint of the returned state.</param>
    internal void Set(string partition, ulong fingerprint)
    {
        if (!_fingerprints.ContainsKey(partition) && _fingerprints.Count >= maxEntries)
        {
            foreach (var key in _fingerprints.Keys)
            {
                _fingerprints.TryRemove(key, out _);
                break;
            }
        }

        _fingerprints[partition] = fingerprint;
    }

    /// <summary>
    /// Check whether a state is the one the client returned for the partition on its previous reduce.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <param name="fingerprint">The fingerprint of the state about to be delivered.</param>
    /// <returns>True if the fingerprint equals the remembered one.</returns>
    internal bool IsUnchanged(string partition, ulong fingerprint) =>
        _fingerprints.TryGetValue(partition, out var remembered) && remembered == fingerprint;

    /// <summary>
    /// Check whether anything is remembered for a partition.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <returns>True if there is an entry.</returns>
    internal bool Has(string partition) => _fingerprints.ContainsKey(partition);

    /// <summary>
    /// Forget a partition.
    /// </summary>
    /// <param name="partition">The partition.</param>
    internal void Remove(string partition) => _fingerprints.TryRemove(partition, out _);

    /// <summary>
    /// Forget every partition.
    /// </summary>
    internal void Clear() => _fingerprints.Clear();
}
