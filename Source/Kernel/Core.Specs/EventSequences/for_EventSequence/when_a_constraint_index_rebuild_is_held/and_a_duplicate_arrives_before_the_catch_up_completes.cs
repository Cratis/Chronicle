// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_is_held;

public class and_a_duplicate_arrives_before_the_catch_up_completes : given.a_held_constraint_index_rebuild
{
    async Task Establish() => await HoldRebuild();

    async Task Because()
    {
        await PassLivenessCheck();
        _result = await AppendValue("current");
        await CompleteCatchUp();
    }

    [Fact] void should_not_admit_the_duplicate() => _result.IsSuccess.ShouldBeFalse();
    [Fact] async Task should_restore_the_history_after_catch_up() => (await Values("source")).ShouldContainOnly(Hash("current"));
}
