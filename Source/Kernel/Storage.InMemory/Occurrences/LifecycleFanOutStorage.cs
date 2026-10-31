// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.Occurrences;

/// <summary>
/// Stores immutable lifecycle snapshots and monotonic progress under one lock.
/// </summary>
public class LifecycleFanOutStorage : ILifecycleFanOutStorage
{
    readonly object _sync = new();
    readonly Dictionary<OccurrenceId, LifecycleFanOutIntent> _intents = [];

    /// <inheritdoc/>
    public Task<LifecycleFanOutIntent?> Get(OccurrenceId id)
    {
        lock (_sync) return Task.FromResult(_intents.TryGetValue(id, out var intent) ? Copy(intent) : null);
    }

    /// <inheritdoc/>
    public Task<bool> TryInsert(LifecycleFanOutIntent intent)
    {
        lock (_sync) return Task.FromResult(_intents.TryAdd(intent.Occurrence.Id, Copy(intent)));
    }

    /// <inheritdoc/>
    public Task<IEnumerable<LifecycleFanOutIntent>> GetPending(int max)
    {
        lock (_sync) return Task.FromResult<IEnumerable<LifecycleFanOutIntent>>(_intents.Values.Where(_ => _.Progress < _.Namespaces.Count).OrderBy(_ => _.Occurrence.Id.Value, StringComparer.Ordinal).Take(Math.Clamp(max, 0, 100)).Select(Copy).ToArray());
    }

    /// <inheritdoc/>
    public Task Advance(OccurrenceId id, int progress)
    {
        lock (_sync)
        {
            if (_intents.TryGetValue(id, out var intent) && progress > intent.Progress && progress <= intent.Namespaces.Count)
            {
                _intents[id] = intent with { Progress = progress };
            }
        }
        return Task.CompletedTask;
    }

    static LifecycleFanOutIntent Copy(LifecycleFanOutIntent intent) => intent with { Namespaces = [.. intent.Namespaces] };
}
