// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_a_removal_names_one_retained_value : given.a_replay
{
    async Task Establish()
    {
        _definition = _definition with { Mode = UniqueConstraintMode.PerValue, RemovalEventDefinitions = [new("Removed", ["email"])] };
        await _claims.Save("source", _definition, 0, Hash("removed"));
        await _claims.Save("source", _definition, 1, Hash("kept"));
        Event(2, "source", "Removed", "removed");
        Start();
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] async Task should_release_only_the_named_value() => (await Values("source")).ShouldContainOnly(Hash("kept").Value);
    [Fact] void should_use_one_value_release() => _writes.ShouldContainOnly($"release:source:{Hash("removed")}");
}
