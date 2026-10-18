// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

using RowEncoding = Cratis.Chronicle.Storage.Observation.ObserverDefinitionChangeRows;

namespace Cratis.Chronicle.Storage.InMemory.Observation;

/// <summary>
/// Stores one row family using atomic compare-and-swap under a lock. Values are copied through the durable encoding.
/// </summary>
/// <typeparam name="T">The payload type.</typeparam>
public class ObserverDefinitionChangeRows<T> : IObserverDefinitionChangeRows<T>
{
    readonly Lock _lock = new();
    readonly SortedDictionary<string, string> _rows = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public Task<ObserverDefinitionChangeRow<T>?> Get(ObserverDefinitionChangeRowKey key)
    {
        lock (_lock)
        {
            return Task.FromResult(_rows.TryGetValue(RowEncoding.Id<T>(key), out var json) ? RowEncoding.Deserialize<T>(json) : null);
        }
    }

    /// <inheritdoc/>
    public Task<bool> TryInsert(ObserverDefinitionChangeRow<T> row)
    {
        lock (_lock)
        {
            return Task.FromResult(row.Version == 0 && RowEncoding.IsValid(row) && _rows.TryAdd(RowEncoding.Id<T>(row.Key), RowEncoding.Serialize(row)));
        }
    }

    /// <inheritdoc/>
    public Task<bool> TryReplace(ObserverDefinitionChangeRow<T> row, long expectedVersion)
    {
        lock (_lock)
        {
            var id = RowEncoding.Id<T>(row.Key);
            if (!RowEncoding.IsReplacement(row, expectedVersion) || !_rows.TryGetValue(id, out var json)) return Task.FromResult(false);
            var stored = RowEncoding.Deserialize<T>(json);
            if (RowEncoding.Identity(stored) != RowEncoding.Identity(row) || stored.Version != expectedVersion || stored.State == ObserverDefinitionChangeRowState.Terminal || stored.State > row.State ||
                (stored.Position is not null && (row.Position is null || stored.Position.Value > row.Position.Value)))
            {
                return Task.FromResult(false);
            }
            _rows[id] = RowEncoding.Serialize(row);

            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task<ObserverDefinitionChangePage<T>> ReadPage(ObserverKey observer, ObserverTypeSetRevision? revision, string? after, int limit)
    {
        if (limit <= 0) return Task.FromException<ObserverDefinitionChangePage<T>>(new InvalidObserverDefinitionChangePageLimit(limit));
        limit = RowEncoding.PageLimit(limit);
        lock (_lock)
        {
            var rows = _rows.Where(pair => after is null || string.CompareOrdinal(pair.Key, after) > 0)
                .Select(pair => (pair.Key, Row: RowEncoding.Deserialize<T>(pair.Value)))
                .Where(pair => pair.Row.Key.Observer == observer && (revision is null || pair.Row.Key.Revision == revision))
                .Take(limit).ToArray();

            return Task.FromResult(new ObserverDefinitionChangePage<T>(rows.Select(pair => pair.Row).ToArray(), rows.Length < limit ? null : rows[^1].Key));
        }
    }

    /// <inheritdoc/>
    public Task<bool> TryDelete(ObserverDefinitionChangeRowKey key, long expectedVersion)
    {
        lock (_lock)
        {
            var id = RowEncoding.Id<T>(key);
            return Task.FromResult(_rows.TryGetValue(id, out var json) && RowEncoding.Deserialize<T>(json).Version == expectedVersion && _rows.Remove(id));
        }
    }
}
