// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Represents an implementation of <see cref="IConnectionOwnership"/>.
/// </summary>
/// <remarks>
/// A connection identifier released by its owner stays reserved for that owner for a retention period, so that
/// another caller cannot take over an identifier that was in use a moment ago.
/// </remarks>
/// <param name="timeProvider">The optional <see cref="TimeProvider"/> for the retention clock.</param>
[Singleton]
public sealed class ConnectionOwnership(TimeProvider? timeProvider = null) : IConnectionOwnership
{
    /// <summary>
    /// How long a released connection identifier stays reserved for its last owner.
    /// </summary>
    internal static readonly TimeSpan ReleasedClaimRetention = TimeSpan.FromHours(1);

    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    long _lastPruneTicks;

    /// <inheritdoc/>
    public ConnectionClaim Claim(ConnectionId connectionId, ConnectionOwner owner)
    {
        string id = connectionId;
        if (owner.IsUnidentified || string.IsNullOrEmpty(id)) return ConnectionClaim.Refused;

        var now = _timeProvider.GetUtcNow();
        Prune(now);

        while (true)
        {
            if (!_entries.TryGetValue(id, out var current))
            {
                if (_entries.TryAdd(id, new(owner, 1, null))) return ConnectionClaim.Claimed;
                continue;
            }

            Entry next;
            if (current.Owner.Equals(owner))
            {
                next = current with { Streams = current.Streams + 1, ReleasedAt = null };
            }
            else if (current.Streams == 0 && now - current.ReleasedAt >= ReleasedClaimRetention)
            {
                next = new(owner, 1, null);
            }
            else
            {
                return ConnectionClaim.Refused;
            }

            if (_entries.TryUpdate(id, next, current)) return ConnectionClaim.Claimed;
        }
    }

    /// <inheritdoc/>
    public void Release(ConnectionId connectionId, ConnectionOwner owner)
    {
        string id = connectionId;
        var now = _timeProvider.GetUtcNow();

        while (_entries.TryGetValue(id, out var current) && current.Owner.Equals(owner) && current.Streams > 0)
        {
            var streams = current.Streams - 1;
            var next = current with { Streams = streams, ReleasedAt = streams == 0 ? now : null };
            if (_entries.TryUpdate(id, next, current)) return;
        }
    }

    /// <inheritdoc/>
    public bool IsOwnedBy(string connectionId, ConnectionOwner owner) =>
        !owner.IsUnidentified &&
        !string.IsNullOrEmpty(connectionId) &&
        _entries.TryGetValue(connectionId, out var entry) &&
        entry.Streams > 0 &&
        entry.Owner.Equals(owner);

    void Prune(DateTimeOffset now)
    {
        var last = Interlocked.Read(ref _lastPruneTicks);
        var interval = ReleasedClaimRetention / 4;
        if (last != 0 && now.UtcTicks - last < interval.Ticks) return;
        if (Interlocked.CompareExchange(ref _lastPruneTicks, now.UtcTicks, last) != last) return;

        foreach (var (id, entry) in _entries)
        {
            if (entry.Streams == 0 && now - entry.ReleasedAt >= ReleasedClaimRetention)
            {
                _entries.TryRemove(new KeyValuePair<string, Entry>(id, entry));
            }
        }
    }

    sealed record Entry(ConnectionOwner Owner, int Streams, DateTimeOffset? ReleasedAt);
}
