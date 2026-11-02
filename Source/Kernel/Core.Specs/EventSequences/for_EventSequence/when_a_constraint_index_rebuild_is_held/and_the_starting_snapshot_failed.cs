// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_is_held;

public class and_the_starting_snapshot_failed : given.a_held_constraint_index_rebuild
{
    async Task Establish()
    {
        _snapshots.BeforeWrite = () => Task.FromException(new given.SimulatedStateWriteError());
        await Catch.Exception(HoldRebuild);
        _snapshots.BeforeWrite = () => Task.CompletedTask;
    }

    async Task Because()
    {
        await PassLivenessCheck();
        await CompleteCatchUp();
        _result = await AppendValue("new-value");
    }

    [Fact] async Task should_find_the_guard_before_it_entered_rebuilding_names() => (await Values("source")).ShouldContainOnly(Hash("current"));
    [Fact] void should_admit_governed_appends_after_catch_up() => _result.IsSuccess.ShouldBeTrue();
}
