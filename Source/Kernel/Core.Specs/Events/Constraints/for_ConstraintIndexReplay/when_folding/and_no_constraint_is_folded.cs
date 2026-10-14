// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_no_constraint_is_folded : given.a_replay
{
    void Establish()
    {
        Event(0, "source", "Claimed", "value");
        Start(noConstraints: true);
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_read_nothing() => _sequence.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_write_nothing() => _writes.ShouldBeEmpty();
    [Fact] void should_complete_without_work() => _result.IsCompleted.ShouldBeTrue();
}
