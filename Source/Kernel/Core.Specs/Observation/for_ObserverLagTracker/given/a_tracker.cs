// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverLagTracker.given;

public class a_tracker : Specification
{
    protected static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    protected static readonly TimeSpan Window = TimeSpan.FromMinutes(3);
    protected static readonly TimeSpan Validity = TimeSpan.FromMinutes(2);

    protected ObserverLagTracker _tracker = new();

    protected ObserverLagSample Measure(int minutes, ulong tail, ulong next = 5, ObserverLagRelevance? relevance = null, ulong handled = 4, ulong count = 4, bool catchingUp = false) =>
        _tracker.Measure(
            new ObserverLagReading((EventSequenceNumber)tail, (EventSequenceNumber)next, (EventSequenceNumber)handled, count, relevance ?? ObserverLagRelevance.Pending((EventSequenceNumber)next), catchingUp),
            Start.AddMinutes(minutes),
            Window,
            Validity);
}
