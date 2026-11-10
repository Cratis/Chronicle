// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Reducers;

/// <summary>
/// Remembers the size the state of recently reduced partitions had, keeping no more than a bounded number of them.
/// </summary>
/// <remarks>
/// Sizes are kept in two generations. New sizes go into the current one, and once it holds half of the capacity it
/// becomes the previous one and the oldest generation is dropped. A size read from the previous generation is moved
/// into the current one, so partitions that keep being reduced stay remembered while those not reduced for a while
/// are forgotten, without ever forgetting everything at once. Counting is done with an interlocked counter, so the
/// hot path never counts the entries of a dictionary.
/// </remarks>
/// <param name="capacity">The most sizes to remember, approximately: writers racing a rotation may add a few more.</param>
public class RememberedStateSizes(int capacity)
{
    readonly int _generationCapacity = Math.Max(1, capacity / 2);
    readonly Lock _rotation = new();
    volatile ConcurrentDictionary<(string Scope, Key Partition), long> _current = new();
    volatile ConcurrentDictionary<(string Scope, Key Partition), long> _previous = new();
    int _currentCount;

    /// <summary>
    /// Gets the number of sizes remembered, counting a size held in both generations twice.
    /// </summary>
    /// <remarks>
    /// Counts every entry, so it is not for the hot path.
    /// </remarks>
    internal int Count => _current.Count + _previous.Count;

    /// <summary>
    /// Try to get the size the state of a partition had the last time it was read.
    /// </summary>
    /// <param name="scope">What the partition belongs to.</param>
    /// <param name="partition">The <see cref="Key"/> of the partition.</param>
    /// <param name="size">The remembered size in bytes, when there is one.</param>
    /// <returns>True when the size is remembered, false if not.</returns>
    public bool TryGet(string scope, Key partition, out long size)
    {
        var key = (scope, partition);
        if (_current.TryGetValue(key, out size))
        {
            return true;
        }

        if (_previous.TryGetValue(key, out size))
        {
            Add(key, size);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Remember the size the state of a partition had when it was read.
    /// </summary>
    /// <param name="scope">What the partition belongs to.</param>
    /// <param name="partition">The <see cref="Key"/> of the partition.</param>
    /// <param name="size">The size in bytes.</param>
    public void Remember(string scope, Key partition, long size) => Add((scope, partition), size);

    void Add((string Scope, Key Partition) key, long size)
    {
        var current = _current;
        if (!current.TryAdd(key, size))
        {
            current[key] = size;
            return;
        }

        if (Interlocked.Increment(ref _currentCount) >= _generationCapacity)
        {
            Rotate(current);
        }
    }

    void Rotate(ConcurrentDictionary<(string Scope, Key Partition), long> full)
    {
        lock (_rotation)
        {
            if (!ReferenceEquals(_current, full))
            {
                return;
            }

            _previous = full;
            _current = new();
            Interlocked.Exchange(ref _currentCount, 0);
        }
    }
}
