// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Coalesces the per-partition handled-event counts of live deliveries.
/// </summary>
/// <remarks>
/// A live delivery adds its counts to the observer's running totals straight away; those travel with the observer
/// state write the delivery makes anyway. Only the per-partition breakdown, which is one more storage write for every
/// delivery, is held back and written for many deliveries at once - after at most
/// <see cref="Configuration.Observers.WriteCoalescingInterval"/>, before a replay and on deactivation, the same as
/// the counts job steps report. Unlike those, these must not be added to the running totals again when flushed.
/// </remarks>
public partial class Observer
{
    Dictionary<Key, Dictionary<EventTypeId, EventCount>> _pendingLivePartitionCounts = [];

    static void AddTo(
        Dictionary<Key, Dictionary<EventTypeId, EventCount>> pendingCounts,
        Key partition,
        IReadOnlyDictionary<EventTypeId, EventCount> countsPerEventType)
    {
        if (!pendingCounts.TryGetValue(partition, out var pending))
        {
            pending = [];
            pendingCounts[partition] = pending;
        }

        foreach (var (eventTypeId, count) in countsPerEventType)
        {
            pending[eventTypeId] = pending.GetValueOrDefault(eventTypeId, EventCount.Zero) + count.Value;
        }
    }

    async Task CoalesceLiveHandledCounts(Key partition, IReadOnlyDictionary<EventTypeId, EventCount> countsPerEventType)
    {
        if (countsPerEventType.Count == 0)
        {
            return;
        }

        AddTo(_pendingLivePartitionCounts, partition, countsPerEventType);
        await FlushOrScheduleHandledCounts();
    }

    /// <summary>
    /// Makes the per-partition counts of live deliveries since the last flush durable.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    async Task FlushPendingLivePartitionCounts()
    {
        if (_pendingLivePartitionCounts.Count == 0)
        {
            return;
        }

        // Serialize with namespace resource deletion and the other handled-count writes.
        await _alertMutationLock.WaitAsync();
        try
        {
            if (IsRetired || _removed || !_observerExists)
            {
                _pendingLivePartitionCounts.Clear();
                return;
            }

            var pending = _pendingLivePartitionCounts;
            _pendingLivePartitionCounts = [];
            var written = new HashSet<Key>();
            try
            {
                var countsStorage = GetObserverHandledCountsStorage();
                foreach (var chunk in pending.Chunk(MaxPendingHandledCountPartitions))
                {
                    await countsStorage.Increment(_observerId, chunk
                        .Select(_ => new KeyValuePair<Key, IReadOnlyDictionary<EventTypeId, EventCount>>(_.Key, _.Value))
                        .ToArray());
                    written.UnionWith(chunk.Select(_ => _.Key));
                }
            }
            catch
            {
                // What was not made durable goes back to be flushed with the next batch, so no count is lost.
                foreach (var (partition, counts) in pending.Where(_ => !written.Contains(_.Key)))
                {
                    AddTo(_pendingLivePartitionCounts, partition, counts);
                }

                throw;
            }
        }
        finally
        {
            _alertMutationLock.Release();
        }
    }
}
