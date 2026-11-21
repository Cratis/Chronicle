// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    readonly Dictionary<Key, EventSequenceNumber> _missedDeliveries = new();
    readonly Dictionary<Key, (EventSequenceNumber First, EventSequenceNumber Last)> _heldBackDeliveries = new();

    EventSequenceNumber CursorBeforePending(Key partition, EventSequenceNumber candidate, EventSequenceNumber? previous = null)
    {
        var before = previous ?? State.NextEventSequenceNumber;
        if (candidate <= before) return candidate;
        var outstanding = Failures.Partitions.Where(_ => _.Partition != partition)
            .Select(_ => _.RecoveryCursor?.Position ?? _.LastAttempt.SequenceNumber)
            .Concat(_heldBackDeliveries.Where(_ => _.Key != partition).Select(_ => _.Value.First));
        foreach (var pending in outstanding.Where(_ => _.IsActualValue && _ < candidate))
        {
            candidate = pending;
        }

        // Existing failures can already be behind the live cursor. They retain their own recovery position;
        // only a missed-delivery repair deliberately rewinds the observer-wide position.
        return candidate < before ? before : candidate;
    }

    bool PartitionHasPendingWork(Key partition) => Failures.IsFailed(partition) ||
        State.CatchingUpPartitions.Contains(partition) || State.ReplayingPartitions.Contains(partition);

    void RememberHeldBackDeliveries(Key partition, IEnumerable<AppendedEvent> events)
    {
        // Legacy replay handover has its own persisted held-back position and is intentionally unchanged.
        if (HeldBackReasonFor(partition) is not { } reason || reason == SuspectedSkipReason.HeldBackWhileReplaying) return;
        foreach (var sequence in events.Where(_ => IsOfInterest(_subscription, State.SubscribesToAllEvents, _)).Select(_ => _.Context.SequenceNumber))
        {
            var range = _heldBackDeliveries.GetValueOrDefault(partition, (First: sequence, Last: sequence));
            _heldBackDeliveries[partition] = (sequence < range.First ? sequence : range.First, sequence > range.Last ? sequence : range.Last);
        }
    }

    void CompleteHeldBackDelivery(Key partition, EventSequenceNumber through)
    {
        if (!through.IsActualValue) return;
        if (_missedDeliveries.TryGetValue(partition, out var missed) && missed <= through) _missedDeliveries.Remove(partition);
        if (!_heldBackDeliveries.TryGetValue(partition, out var range) || through < range.First) return;
        if (through >= range.Last)
        {
            _heldBackDeliveries.Remove(partition);
        }
        else
        {
            // A conservative lower bound is sufficient: the catch-up reader selects applicable events.
            _heldBackDeliveries[partition] = (through.Next(), range.Last);
        }
    }

    void QueueMissedDelivery(Key partition, EventSequenceNumber first)
    {
        if (!_missedDeliveries.TryGetValue(partition, out var pending) || first < pending)
        {
            _missedDeliveries[partition] = first;
        }
    }

    void QueueSuspectedSkip(Key partition, EventSequenceNumber first, int count, SuspectedSkipReason reason)
    {
        QueueMissedDelivery(partition, first);
        ReportSuspectedSkip(partition, first, count, reason);
    }

    async Task<bool> RepairDiscardedDeliveries(Key partition, IEnumerable<AppendedEvent> events, EventSequenceNumber next)
    {
        NoteDiscardedEvents(partition, events, next);
        if (!_missedDeliveries.ContainsKey(partition)) return false;
        await RepairMissedDeliveries();
        return true;
    }

    void QueueUnownedHeldBackDeliveries()
    {
        foreach (var (partition, range) in _heldBackDeliveries.Where(_ => !PartitionHasPendingWork(_.Key)))
        {
            QueueMissedDelivery(partition, range.First);
        }
    }

    bool MissedDeliveryAlreadyOwned(Key partition)
    {
        if (!PartitionHasPendingWork(partition)) return false;
        _missedDeliveries.Remove(partition);
        return true;
    }

    async Task RepairMissedDeliveries()
    {
        if (!_isPreparingCatchup) QueueUnownedHeldBackDeliveries();
        foreach (var (partition, first) in _missedDeliveries.OrderBy(_ => _.Value).ToArray())
        {
            if (!CanRepairMissedDeliveries()) return;
            if (State.InFlightPartitions.Contains(partition) || MissedDeliveryAlreadyOwned(partition)) continue;
            await RepairMissedDelivery(partition, first);
        }
    }

    bool CanRepairMissedDeliveries() => IsObserverLive() && IsActiveAndNotQuarantined() && _subscription.IsSubscribed;

    async Task RepairMissedDelivery(Key partition, EventSequenceNumber first)
    {
        // Persist the rewind before starting a job. Even if starting it fails or activation ends here,
        // reconnect cannot resume beyond the acknowledged event that has not been delivered.
        if (first < State.NextEventSequenceNumber)
        {
            State = State with { NextEventSequenceNumber = first };
            await WriteStateAsync();
        }

        try
        {
            await StartCatchupJob(partition, first);
        }
        catch (Exception exception)
        {
            State.CatchingUpPartitions.Remove(partition);
            _partitionHoldsVersion++;
            await PartitionFailed(partition, first, [exception.Message], exception.StackTrace ?? string.Empty);
        }

        _missedDeliveries.Remove(partition);
    }
}
