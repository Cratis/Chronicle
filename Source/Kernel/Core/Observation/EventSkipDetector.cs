// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Detects live events an observer's position moved past without any delivery path covering them. The result is a
/// suspicion, never proof.
/// </summary>
/// <remarks>
/// <para>
/// The detector is bookkeeping only: it never writes state, never reads storage and never changes what the observer
/// does. It is not a grain and takes no lock, because every call is synchronous and the observer's turns run on one
/// scheduler. All collections are bounded, and every bound only ever means fewer reports: when coverage can no longer
/// be tracked, an unknown-coverage ceiling suppresses reports at or below it, and saturated ownership suppresses the
/// rest. A failure inside the detector disables it for the activation.
/// </para>
/// <para>
/// Two signals are reported:
/// held-back events the position later moved past (the position crossed an event held back while the partition was
/// catching up, replaying or preparing catch-up), and events discarded as already handled after another partition's
/// job moved the position past them.
/// </para>
/// </remarks>
/// <param name="isOwnedByObserver">Whether the observer itself still considers the partition owned by a catch-up, replay or failure.</param>
/// <param name="report">Called with the partition, the first sequence number, the number of suspected events and the reason.</param>
internal sealed class EventSkipDetector(Func<Key, bool> isOwnedByObserver, Action<Key, EventSequenceNumber, int, SuspectedSkipReason> report)
{
    /// <summary>
    /// The maximum number of held-back sequence numbers tracked for one partition.
    /// </summary>
    internal const int MaxSequenceNumbersPerPartition = 16;

    /// <summary>
    /// The maximum number of partitions tracked by each of the detector's partition collections.
    /// </summary>
    internal const int MaxTrackedPartitions = 256;

    /// <summary>
    /// The number of recent partition-scoped advances of the observer's position that are remembered.
    /// </summary>
    internal const int MaxAdvances = 32;

    readonly Dictionary<Key, HeldBackEvents> _pending = [];
    readonly Dictionary<Key, long> _jobOwned = [];
    readonly HashSet<Key> _jobReported = [];
    readonly Dictionary<Key, ulong> _reach = [];
    readonly Queue<Advance> _advances = new();
    ulong _pendingMinimum = ulong.MaxValue;
    long _tickets;
    ulong? _unknownCoverageCeiling;
    ulong? _observerWideThrough;
    bool _jobOwnedSaturated;
    bool _jobReportedSaturated;
    bool _faulted;

    /// <summary>
    /// Records partitions already owned by a job when the observer starts counting, such as in-flight recoveries.
    /// </summary>
    /// <param name="partitions">The partitions owned by a job.</param>
    internal void SeedJobOwned(IEnumerable<Key> partitions) => Run(() =>
    {
        foreach (var partition in partitions)
        {
            Own(partition);
        }
    });

    /// <summary>
    /// Records that a job started for a partition, so the partition counts as owned until it is released.
    /// </summary>
    /// <param name="partition">The partition.</param>
    internal void JobStarted(Key partition) => Run(() => Own(partition));

    /// <summary>
    /// Records that live events were held back instead of being delivered.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <param name="sequenceNumbers">The sequence numbers of the held back events the observer is interested in.</param>
    /// <param name="reason">Why they were held back.</param>
    internal void HeldBack(Key partition, IEnumerable<EventSequenceNumber> sequenceNumbers, SuspectedSkipReason reason) => Run(() =>
    {
        if (_jobReportedSaturated || _jobReported.Contains(partition))
        {
            return;
        }

        foreach (var sequenceNumber in sequenceNumbers)
        {
            if (!IsCovered(partition, sequenceNumber.Value))
            {
                Track(partition, sequenceNumber.Value, reason);
            }
        }
    });

    /// <summary>
    /// Records that live events were discarded because they lie below the observer's position, and reports the ones
    /// another partition's job moved the position past.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <param name="sequenceNumbers">The ascending sequence numbers of the discarded events the observer is interested in.</param>
    /// <param name="next">The observer's next event sequence number.</param>
    internal void DiscardedBelowNext(Key partition, IEnumerable<EventSequenceNumber> sequenceNumbers, EventSequenceNumber next) => Run(() =>
    {
        if (!CanSuspectDiscards(partition))
        {
            return;
        }

        var suspected = sequenceNumbers
            .Select(_ => _.Value)
            .Where(_ => _ < next.Value && IsUncoveredAndPassed(partition, _))
            .Order()
            .ToArray();
        if (suspected.Length > 0)
        {
            report(partition, suspected[0], suspected.Length, SuspectedSkipReason.PassedByPartitionJob);
        }
    });

    /// <summary>
    /// Records that a job reported handled events for a partition. Its held back events are no longer tracked.
    /// </summary>
    /// <param name="partition">The partition.</param>
    internal void JobReported(Key partition) => Run(() =>
    {
        _pending.Remove(partition);
        RecomputePendingMinimum();
        if (_jobReported.Count >= MaxTrackedPartitions && !_jobReported.Contains(partition))
        {
            _jobReportedSaturated = true;
            return;
        }

        _jobReported.Add(partition);
    });

    /// <summary>
    /// Forgets which partitions were reported by jobs. Called when a catch-up round hands over.
    /// </summary>
    internal void ClearJobReported() => Run(() =>
    {
        _jobReported.Clear();
        _jobReportedSaturated = false;
    });

    /// <summary>
    /// Records that a partition job is completing and keeps the partition owned until the decision about a successor is made.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <param name="reach">The furthest event the job read or handled.</param>
    internal void Completing(Key partition, EventSequenceNumber reach) => Run(() =>
    {
        Own(partition);
        if (!reach.IsActualValue)
        {
            return;
        }

        RecordReach(partition, reach.Value);
        RemovePendingAtOrBelow(partition, reach.Value);
    });

    /// <summary>
    /// Records that a partition job delivered events up to a position but has not completed.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <param name="reach">The furthest event the job handled.</param>
    internal void Delivered(Key partition, EventSequenceNumber reach) => Run(() =>
    {
        if (reach.IsActualValue)
        {
            RecordReach(partition, reach.Value);
        }
    });

    /// <summary>
    /// Gets the current ownership ticket of a partition, to hand back to <see cref="Release"/> when the decision about a successor is settled.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <returns>The ticket, or zero if the detector does not hold the partition.</returns>
    internal long OwnershipTicket(Key partition) => !_faulted && _jobOwned.TryGetValue(partition, out var ticket) ? ticket : 0;

    /// <summary>
    /// Records that a storage probe for a partition starts from a position, which covers its held back events from there on.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <param name="from">The position the probe starts from.</param>
    internal void ProbedFrom(Key partition, EventSequenceNumber from) => Run(() =>
    {
        if (from.IsActualValue && _pending.TryGetValue(partition, out var held))
        {
            held.RemoveAtOrAbove(from.Value);
            DropWhenEmpty(partition, held);
        }
    });

    /// <summary>
    /// Releases a partition once the decision about its successor is settled, unless a job started for it in the meantime.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <param name="ticket">The ticket from <see cref="OwnershipTicket"/>.</param>
    internal void Release(Key partition, long ticket) => Run(() =>
    {
        if (_jobOwned.TryGetValue(partition, out var current) && current == ticket)
        {
            _jobOwned.Remove(partition);
        }
    });

    /// <summary>
    /// Records that a partition-scoped event moved the observer's position.
    /// </summary>
    /// <param name="partition">The partition whose event moved the position.</param>
    /// <param name="before">The position before.</param>
    /// <param name="after">The position after.</param>
    internal void Advanced(Key partition, EventSequenceNumber before, EventSequenceNumber after) => Run(() =>
    {
        if (!IsForwardMove(before, after))
        {
            return;
        }

        _advances.Enqueue(new(partition, before.Value, after.Value));
        while (_advances.Count > MaxAdvances)
        {
            _advances.Dequeue();
        }
    });

    /// <summary>
    /// Records that an observer-wide catch-up round or replay reached a position.
    /// </summary>
    /// <param name="through">The furthest position reported.</param>
    internal void RoundConcluded(EventSequenceNumber through) => Run(() =>
    {
        if (ExtendsObserverWideReach(through))
        {
            _observerWideThrough = through.Value;
        }
    });

    /// <summary>
    /// Records that catch-up preparation ended, whether by a round handing over or by any path that lowers it. Events held
    /// back while it was preparing were owned by it until now.
    /// </summary>
    internal void PreparationEnded() => Run(() =>
    {
        foreach (var held in _pending.Values)
        {
            held.ReleasePreparation();
        }
    });

    /// <summary>
    /// Reports held back events the observer's position has moved past and that nothing covers.
    /// </summary>
    /// <param name="next">The observer's next event sequence number.</param>
    internal void Evaluate(EventSequenceNumber next)
    {
        if (!MayHavePendingBelow(next))
        {
            return;
        }

        Run(() =>
        {
            var reports = new List<(Key Partition, ulong First, int Count, SuspectedSkipReason Reason)>();
            foreach (var partition in _pending.Keys.ToArray())
            {
                EvaluatePartition(partition, next.Value, reports);
            }

            RecomputePendingMinimum();
            foreach (var (partition, first, count, reason) in reports)
            {
                report(partition, first, count, reason);
            }
        });
    }

    /// <summary>
    /// Forgets everything the detector knows.
    /// </summary>
    internal void Reset() => Run(() =>
    {
        _pending.Clear();
        _jobOwned.Clear();
        _jobReported.Clear();
        _reach.Clear();
        _advances.Clear();
        _pendingMinimum = ulong.MaxValue;
        _unknownCoverageCeiling = null;
        _observerWideThrough = null;
        _jobOwnedSaturated = false;
        _jobReportedSaturated = false;
    });

    static bool IsForwardMove(EventSequenceNumber before, EventSequenceNumber after) =>
        before.IsActualValue && after.IsActualValue && after.Value > before.Value;

    void Run(Action action)
    {
        if (_faulted)
        {
            return;
        }

        try
        {
            action();
        }
        catch (Exception ex) when (ObservationOnlyFailures.CanBeContained(ex))
        {
            // The detector only ever suspects, so a failure of its own disables it rather than the observer.
            _faulted = true;
            _pending.Clear();
        }
    }

    void Own(Key partition)
    {
        if (!_jobOwned.ContainsKey(partition) && _jobOwned.Count >= MaxTrackedPartitions)
        {
            _jobOwnedSaturated = true;
            return;
        }

        _jobOwned[partition] = ++_tickets;
    }

    bool CanSuspectDiscards(Key partition) =>
        !IsSaturated() && _advances.Count > 0 && !IsOwned(partition);

    bool IsSaturated() => _jobOwnedSaturated || _jobReportedSaturated;

    bool IsUncoveredAndPassed(Key partition, ulong sequenceNumber) =>
        !IsCovered(partition, sequenceNumber) &&
        !IsObserverWideCovered(sequenceNumber) &&
        WasPassedByAnotherPartition(partition, sequenceNumber);

    bool ExtendsObserverWideReach(EventSequenceNumber through) =>
        through.IsActualValue && (_observerWideThrough is null || through.Value > _observerWideThrough);

    bool MayHavePendingBelow(EventSequenceNumber next) =>
        !_faulted && _pending.Count > 0 && next.Value > _pendingMinimum;

    bool IsOwned(Key partition) =>
        _jobOwnedSaturated || _jobOwned.ContainsKey(partition) || isOwnedByObserver(partition);

    bool IsCovered(Key partition, ulong sequenceNumber) =>
        (_unknownCoverageCeiling is { } ceiling && sequenceNumber <= ceiling) ||
        (_reach.TryGetValue(partition, out var reach) && sequenceNumber <= reach);

    bool IsObserverWideCovered(ulong sequenceNumber) => _observerWideThrough is { } through && sequenceNumber <= through;

    bool WasPassedByAnotherPartition(Key partition, ulong sequenceNumber) =>
        _advances.Any(_ => _.Partition != partition && _.Before <= sequenceNumber && sequenceNumber < _.After);

    void Track(Key partition, ulong sequenceNumber, SuspectedSkipReason reason)
    {
        if (!_pending.TryGetValue(partition, out var held))
        {
            if (_pending.Count >= MaxTrackedPartitions)
            {
                return;
            }

            held = new();
            _pending[partition] = held;
        }

        if (held.Add(sequenceNumber, reason) && sequenceNumber < _pendingMinimum)
        {
            _pendingMinimum = sequenceNumber;
        }
    }

    void RecordReach(Key partition, ulong reach)
    {
        if (_reach.TryGetValue(partition, out var existing))
        {
            _reach[partition] = Math.Max(existing, reach);
            return;
        }

        if (_reach.Count < MaxTrackedPartitions)
        {
            _reach[partition] = reach;
            return;
        }

        var lowest = _reach.MinBy(_ => _.Value);
        if (reach > lowest.Value)
        {
            _reach.Remove(lowest.Key);
            _reach[partition] = reach;
            RaiseCeiling(lowest.Value);
            return;
        }

        RaiseCeiling(reach);
    }

    void RaiseCeiling(ulong lostCoverage)
    {
        if (_unknownCoverageCeiling is null || lostCoverage > _unknownCoverageCeiling)
        {
            _unknownCoverageCeiling = lostCoverage;
        }
    }

    void RemovePendingAtOrBelow(Key partition, ulong reach)
    {
        if (_pending.TryGetValue(partition, out var held))
        {
            held.RemoveAtOrBelow(reach);
            DropWhenEmpty(partition, held);
        }
    }

    void DropWhenEmpty(Key partition, HeldBackEvents held)
    {
        if (held.IsEmpty)
        {
            _pending.Remove(partition);
        }

        RecomputePendingMinimum();
    }

    void RecomputePendingMinimum() =>
        _pendingMinimum = _pending.Count == 0 ? ulong.MaxValue : _pending.Values.Min(_ => _.Minimum);

    void EvaluatePartition(Key partition, ulong next, List<(Key Partition, ulong First, int Count, SuspectedSkipReason Reason)> reports)
    {
        var held = _pending[partition];
        var crossed = held.TakeBelow(next);
        var canReport = !_jobReportedSaturated && !IsOwned(partition);
        var uncovered = crossed.Where(_ => !IsCovered(partition, _.SequenceNumber)).ToArray();
        var unowned = canReport ? uncovered.Where(_ => !_.OwnedByPreparation).ToArray() : [];
        held.Restore(uncovered.Except(unowned));
        foreach (var group in unowned.GroupBy(_ => _.Reason))
        {
            reports.Add((partition, group.Min(_ => _.SequenceNumber), group.Count(), group.Key));
        }

        if (held.IsEmpty)
        {
            _pending.Remove(partition);
        }
    }

    readonly record struct Advance(Key Partition, ulong Before, ulong After);

    readonly record struct HeldEvent(ulong SequenceNumber, SuspectedSkipReason Reason, bool OwnedByPreparation);

    sealed class HeldEventComparer : IComparer<HeldEvent>
    {
        internal static readonly HeldEventComparer Instance = new();

        public int Compare(HeldEvent x, HeldEvent y) => x.SequenceNumber.CompareTo(y.SequenceNumber);
    }

    sealed class HeldBackEvents
    {
        readonly List<HeldEvent> _events = [];

        public bool IsEmpty => _events.Count == 0;

        public ulong Minimum => _events[0].SequenceNumber;

        public bool Add(ulong sequenceNumber, SuspectedSkipReason reason)
        {
            var index = _events.BinarySearch(new(sequenceNumber, reason, false), HeldEventComparer.Instance);
            if (index >= 0 || _events.Count >= MaxSequenceNumbersPerPartition)
            {
                return false;
            }

            _events.Insert(~index, new(sequenceNumber, reason, reason == SuspectedSkipReason.HeldBackWhilePreparingCatchUp));
            return true;
        }

        public HeldEvent[] TakeBelow(ulong next)
        {
            var taken = _events.Where(_ => _.SequenceNumber < next).ToArray();
            _events.RemoveRange(0, taken.Length);
            return taken;
        }

        public void Restore(IEnumerable<HeldEvent> events)
        {
            foreach (var @event in events)
            {
                var index = _events.BinarySearch(@event, HeldEventComparer.Instance);
                if (index < 0 && _events.Count < MaxSequenceNumbersPerPartition)
                {
                    _events.Insert(~index, @event);
                }
            }
        }

        public void ReleasePreparation()
        {
            for (var i = 0; i < _events.Count; i++)
            {
                _events[i] = _events[i] with { OwnedByPreparation = false };
            }
        }

        public void RemoveAtOrBelow(ulong position) => _events.RemoveAll(_ => _.SequenceNumber <= position);

        public void RemoveAtOrAbove(ulong position) => _events.RemoveAll(_ => _.SequenceNumber >= position);
    }
}
