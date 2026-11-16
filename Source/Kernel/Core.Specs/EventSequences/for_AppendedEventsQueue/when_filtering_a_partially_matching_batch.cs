// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue;

public class when_filtering_a_partially_matching_batch : given.subscription_matching
{
    void Because() => Filter();

    [Fact] void should_retain_matching_events_in_order() => _result.ShouldEqual([_events[0], _events[2]]);
    [Fact] void should_allocate_an_exact_sized_array() => _result.GetType().ShouldEqual(_events.GetType());
    [Fact] void should_not_modify_the_input() => _events.Length.ShouldEqual(3);
    [Fact] void should_not_share_the_input_array() => ReferenceEquals(_result, _events).ShouldBeFalse();
}
