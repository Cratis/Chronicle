// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_per_value_collect_uses_point_reads : given.a_replay
{
    void Establish()
    {
        _definition = _definition with { Mode = UniqueConstraintMode.PerValue };
        Event(0, "source", "Claimed", "one");
        Event(1, "source", "Claimed", "two");
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_read_each_value_once() => _unique.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(_unique.IsAllowed)).ShouldEqual(2);
    [Fact] void should_not_load_the_sources_complete_values() => _unique.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(_unique.GetFor)).ShouldEqual(0);
}
