// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Persists handled-count chunks and retains only their unconfirmed suffixes for retry.
/// </summary>
public partial class Observer
{
    /// <summary>
    /// Makes reported handled-event counts durable, with one state write for all of them.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    async Task FlushPendingHandledCounts()
    {
        await FlushPendingLivePartitionCounts();
        if (_pendingHandledCounts.Count == 0)
        {
            return;
        }

        // Serialize independent handled-count writes with namespace resource deletion as well as source writes.
        await _alertMutationLock.WaitAsync();
        try
        {
            if (IsRetired || _removed || !_observerExists)
            {
                _pendingHandledCounts.Clear();
                return;
            }

            if (await RelinquishOwnershipIfMoved()) return;

            await FlushReportedHandledCounts();
        }
        finally
        {
            _alertMutationLock.Release();
        }
    }

    async Task FlushReportedHandledCounts()
    {
        _handledEventsReports.ForgetQuietSteps();
        var pending = _pendingHandledCounts;
        _pendingHandledCounts = [];
        var written = new HashSet<Key>();
        try
        {
            var countsStorage = GetObserverHandledCountsStorage();
            foreach (var chunk in pending.Chunk(MaxPendingHandledCountPartitions))
            {
                await PersistHandledCountChunk(countsStorage, chunk, written, updateRunningTotals: true);
            }

            await WriteStateAsync();
        }
        catch
        {
            // Confirmed increments are already durable, even when a later state write fails.
            RestorePendingHandledCounts(pending.Where(_ => !written.Contains(_.Key)));
            throw;
        }
    }

    async Task PersistHandledCountChunk(
        IObserverHandledCountsStorage countsStorage,
        KeyValuePair<Key, Dictionary<EventTypeId, EventCount>>[] chunk,
        HashSet<Key> written,
        bool updateRunningTotals)
    {
        try
        {
            await countsStorage.Increment(_observerId, chunk
                .Select(_ => new KeyValuePair<Key, IReadOnlyDictionary<EventTypeId, EventCount>>(_.Key, _.Value))
                .ToArray());
        }
        catch (ObserverHandledCountsPartiallyIncremented exception) when (exception.AppliedCount >= 0 && exception.AppliedCount < chunk.Length)
        {
            RecordConfirmedHandledCounts(chunk.Take(exception.AppliedCount), written, updateRunningTotals);
            throw;
        }

        RecordConfirmedHandledCounts(chunk, written, updateRunningTotals);
    }

    void RecordConfirmedHandledCounts(
        IEnumerable<KeyValuePair<Key, Dictionary<EventTypeId, EventCount>>> countsPerPartition,
        HashSet<Key> written,
        bool updateRunningTotals)
    {
        foreach (var (partition, counts) in countsPerPartition)
        {
            written.Add(partition);
            if (updateRunningTotals)
            {
                State = WithIncrementedRunningTotals(State, counts);
                RecordedHandlingState();
            }
        }
    }
}
