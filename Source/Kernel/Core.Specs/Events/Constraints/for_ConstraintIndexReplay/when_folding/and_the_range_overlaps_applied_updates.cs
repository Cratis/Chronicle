// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_the_range_overlaps_applied_updates : given.a_replay
{
    async Task Establish()
    {
        Event(0, "source", "Claimed", "old");
        Event(1, "source", "Claimed", "current");
        await _claims.Save("source", _definition, 1, Hash("current"));
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_write_nothing() => _writes.ShouldBeEmpty();
    [Fact] void should_complete_without_reapplying_current_keys() => _result.IsCompleted.ShouldBeTrue();
}
