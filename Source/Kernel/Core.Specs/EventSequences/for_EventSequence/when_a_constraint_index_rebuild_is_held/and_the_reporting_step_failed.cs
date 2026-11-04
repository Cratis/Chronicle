// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_is_held;

public class and_the_reporting_step_failed : given.a_held_constraint_index_rebuild
{
    AppendResult _duplicate;

    async Task Establish() => await HoldRebuild();

    async Task Because()
    {
        await PassLivenessCheck();
        await CompleteCatchUp();
        _result = await AppendValue("new-value");
        _duplicate = await AppendValue("current");
    }

    [Fact] async Task should_restore_the_cleared_history() => (await Values("source")).ShouldContainOnly(Hash("current"));
    [Fact] void should_admit_a_new_value() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_refuse_a_duplicate_from_another_source() => _duplicate.ConstraintViolations.ShouldNotBeEmpty();
}
