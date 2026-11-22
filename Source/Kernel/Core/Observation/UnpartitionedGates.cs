// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The per-silo gates that let one caller at a time deliver to an unpartitioned subscriber.
/// </summary>
/// <remarks>
/// A gate exists only while somebody holds or awaits it. The last <see cref="Lease"/> to be disposed removes the
/// entry, so the table holds at most one gate for every subscriber that is delivering right now, rather than one for
/// every subscriber that ever delivered.
/// </remarks>
internal sealed class UnpartitionedGates
{
    readonly Dictionary<string, Entry> _entries = [];

    /// <summary>
    /// Gets the number of gates currently retained.
    /// </summary>
    internal int Count
    {
        get
        {
            lock (_entries)
            {
                return _entries.Count;
            }
        }
    }

    /// <summary>
    /// Gets whether a gate is retained for the key.
    /// </summary>
    /// <param name="key">The key of the subscriber.</param>
    /// <returns>True when a gate is retained.</returns>
    internal bool Has(string key)
    {
        lock (_entries)
        {
            return _entries.ContainsKey(key);
        }
    }

    /// <summary>
    /// Takes a lease on the gate for the key, creating the gate when nobody else has one.
    /// </summary>
    /// <param name="key">The key of the subscriber.</param>
    /// <returns>The <see cref="Lease"/>, to be disposed once the caller is done with the gate.</returns>
    internal Lease Acquire(string key)
    {
        lock (_entries)
        {
            if (!_entries.TryGetValue(key, out var entry))
            {
                entry = new Entry();
                _entries[key] = entry;
            }

            entry.Leases++;
            return new Lease(this, key, entry);
        }
    }

    void Release(string key, Entry entry)
    {
        lock (_entries)
        {
            if (--entry.Leases == 0)
            {
                _entries.Remove(key);
            }
        }
    }

    sealed class Entry
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);

        public int Leases { get; set; }
    }

    /// <summary>
    /// A claim on a gate; the gate is kept for as long as any lease on it is undisposed.
    /// </summary>
    /// <param name="owner">The owning <see cref="UnpartitionedGates"/>.</param>
    /// <param name="key">The key of the gate.</param>
    /// <param name="entry">The retained entry.</param>
    internal sealed class Lease(UnpartitionedGates owner, string key, object entry) : IDisposable
    {
        int _disposed;

        /// <summary>
        /// Waits to be the one caller inside the gate.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> for cancelling the wait.</param>
        /// <returns>A <see cref="Task"/> that completes when the caller is inside.</returns>
        internal Task Enter(CancellationToken cancellationToken) => ((Entry)entry).Gate.WaitAsync(cancellationToken);

        /// <summary>
        /// Leaves the gate so the next caller can enter.
        /// </summary>
        internal void Exit() => ((Entry)entry).Gate.Release();

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                owner.Release(key, (Entry)entry);
            }
        }
    }
}
