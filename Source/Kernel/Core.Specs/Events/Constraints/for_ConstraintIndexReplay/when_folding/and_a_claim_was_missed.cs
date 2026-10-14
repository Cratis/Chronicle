// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_claim_was_missed : given.a_replay
{
    void Establish()
    {
        Event(0, "source", "Claimed", "value");
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] async Task should_repair_the_claim() => (await Values("source")).ShouldContainOnly(Hash("value").Value);
    [Fact] void should_complete_the_range() => _result.IsCompleted.ShouldBeTrue();
}
