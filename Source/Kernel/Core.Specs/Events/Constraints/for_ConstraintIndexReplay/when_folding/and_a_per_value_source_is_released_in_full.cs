// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_per_value_source_is_released_in_full : given.a_replay
{
    async Task Establish()
    {
        _definition = _definition with { Mode = UniqueConstraintMode.PerValue };
        await _claims.Save("source", _definition, 0, Hash("first"));
        await _claims.Save("source", _definition, 1, Hash("second"));
        Event(2, "source", "Claimed", "third");
        Event(3, "source", "Removed");
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] async Task should_release_every_stored_value() => (await Values("source")).ShouldBeEmpty();
    [Fact] void should_never_write_the_intermediate_claim() => _writes.ShouldNotContain($"claim:source:{Hash("third")}");
    [Fact] void should_release_each_stored_value_once() => _writes.Count.ShouldEqual(2);
}
