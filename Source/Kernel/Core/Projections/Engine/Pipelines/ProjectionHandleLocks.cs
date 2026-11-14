// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>
/// Registry of <see cref="ProjectionHandleLock"/> instances that holds them weakly.
/// </summary>
/// <remarks>
/// A lock lives exactly as long as some pipeline references it. Two pipelines of one projection that are alive at the
/// same time (an old one still held by a subscriber and its replacement) therefore always resolve the same lock,
/// while the locks and keys of projections nobody uses any more are released instead of accumulating per namespace.
/// </remarks>
internal sealed class ProjectionHandleLocks
{
    const int PruneEvery = 256;

    readonly Lock _gate = new();
    readonly Dictionary<string, WeakReference<ProjectionHandleLock>> _locks = [];
    int _sinceLastPrune;

    /// <summary>Gets the number of keys currently registered, including keys whose lock has not been pruned yet.</summary>
    internal int Count
    {
        get
        {
            lock (_gate) return _locks.Count;
        }
    }

    /// <summary>Gets the number of keys whose lock is still alive.</summary>
    internal int AliveCount
    {
        get
        {
            lock (_gate) return _locks.Values.Count(reference => reference.TryGetTarget(out _));
        }
    }

    /// <summary>Gets the lock for a key, creating it when no live lock exists.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The shared <see cref="ProjectionHandleLock"/>.</returns>
    internal ProjectionHandleLock GetFor(string key)
    {
        lock (_gate)
        {
            if (++_sinceLastPrune >= PruneEvery)
            {
                _sinceLastPrune = 0;
                Prune();
            }

            if (_locks.TryGetValue(key, out var weak) && weak.TryGetTarget(out var existing)) return existing;

            var created = new ProjectionHandleLock();
            _locks[key] = new WeakReference<ProjectionHandleLock>(created);
            return created;
        }
    }

    /// <summary>Removes the key.</summary>
    /// <param name="key">The key.</param>
    internal void Remove(string key)
    {
        lock (_gate) _locks.Remove(key);
    }

    /// <summary>Removes the keys whose lock has been collected.</summary>
    internal void Prune()
    {
        lock (_gate)
        {
            foreach (var key in _locks.Where(pair => !pair.Value.TryGetTarget(out _)).Select(pair => pair.Key).ToArray()) _locks.Remove(key);
        }
    }
}
