// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_EventSkipDetector.given;

public class a_detector : Specification
{
    protected static readonly Key _q = "Q";
    protected static readonly Key _p = "P";

    private protected EventSkipDetector _detector;
    private protected List<Report> _reports;
    protected HashSet<Key> _ownedByObserver;

    private protected record Report(Key Partition, EventSequenceNumber First, int Count, SuspectedSkipReason Reason);

    void Establish()
    {
        _reports = [];
        _ownedByObserver = [];
        _detector = new(_ownedByObserver.Contains, (partition, first, count, reason) => _reports.Add(new(partition, first, count, reason)));
    }

    protected void HoldBack(Key partition, params ulong[] sequenceNumbers) =>
        _detector.HeldBack(partition, sequenceNumbers.Select(_ => (EventSequenceNumber)_), SuspectedSkipReason.HeldBackWhileCatchingUp);

    protected void Discard(Key partition, EventSequenceNumber next, params ulong[] sequenceNumbers) =>
        _detector.DiscardedBelowNext(partition, sequenceNumbers.Select(_ => (EventSequenceNumber)_), next);

    protected void FillReachTracking(ulong firstReach, int count)
    {
        for (var i = 0; i < count; i++)
        {
            _detector.Delivered($"filler-{i}", firstReach + (ulong)i);
        }
    }
}
