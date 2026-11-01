// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverDeliveryRuns;

public class when_forming_an_ordered_run : given.a_controlled_subscription
{
    AppendedEvent[] _run;

    void Because() => _run = ObserverDeliveryRuns.Take(_old, [Event(_a, 1), Event(_e, 2), Event(_a, 3)]);

    [Fact] void should_not_skip_the_uncovered_event_to_deliver_a_later_one() => _run.Length.ShouldEqual(1);
    [Fact] void should_stop_before_the_first_uncovered_event() => _run[0].Context.SequenceNumber.ShouldEqual((EventSequenceNumber)1);
}
