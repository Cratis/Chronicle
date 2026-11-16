// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_incrementing_running_totals : given.running_totals
{
    void Because() => _result = Increment(_state, new Dictionary<Concepts.Events.EventTypeId, Concepts.Events.EventCount> { ["first"] = 2UL, ["third"] = 4UL });

    [Fact] void should_increment_the_total() => _result.HandledEventCount.Value.ShouldEqual(14UL);
    [Fact] void should_increment_existing_types() => _result.HandledEventCountPerEventType["first"].Value.ShouldEqual(5UL);
    [Fact] void should_add_new_types() => _result.HandledEventCountPerEventType["third"].Value.ShouldEqual(4UL);
    [Fact] void should_not_mutate_the_input_dictionary() => _state.HandledEventCountPerEventType["first"].Value.ShouldEqual(3UL);
}
