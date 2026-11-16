// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_subtracting_partition_totals : given.running_totals
{
    void Because() => _result = Subtract(_state, new Dictionary<EventTypeId, EventCount> { ["first"] = 4UL, ["second"] = 2UL, ["missing"] = 9UL });

    [Fact] void should_clamp_the_total_at_zero() => _result.HandledEventCount.Value.ShouldEqual(0UL);
    [Fact] void should_remove_exhausted_type_totals() => _result.HandledEventCountPerEventType.ContainsKey("first").ShouldBeFalse();
    [Fact] void should_subtract_other_type_totals() => _result.HandledEventCountPerEventType["second"].Value.ShouldEqual(3UL);
    [Fact] void should_not_add_missing_types() => _result.HandledEventCountPerEventType.ContainsKey("missing").ShouldBeFalse();
    [Fact] void should_not_mutate_the_input_dictionary() => _state.HandledEventCountPerEventType["first"].Value.ShouldEqual(3UL);
}
