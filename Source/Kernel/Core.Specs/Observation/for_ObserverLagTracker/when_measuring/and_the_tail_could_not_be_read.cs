// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverLagTracker.when_measuring;

public class and_the_tail_could_not_be_read : given.a_tracker
{
    ObserverLagSample _result;

    void Because() => _result = _tracker.Measure(new ObserverLagReading(EventSequenceNumber.Unavailable, (EventSequenceNumber)5UL, (EventSequenceNumber)4UL, 4, ObserverLagRelevance.Unknown, false), Start, Window, Validity);

    [Fact] void should_be_unmeasurable() => _result.Status.ShouldEqual(ObserverLagStatus.Unmeasurable);
    [Fact] void should_not_claim_a_distance() => _result.Distance.ShouldBeNull();
    [Fact] void should_say_the_tail_is_unavailable() => _result.Cause.ShouldEqual(ObserverLagCause.TailUnavailable);
}
