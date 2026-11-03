// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_is_held;

public class and_the_reporting_step_vanished : given.a_held_constraint_index_rebuild
{
    async Task Establish()
    {
        await HoldRebuild();
        _steps.Clear();
    }

    async Task Because()
    {
        await PassLivenessCheck();
        await CompleteCatchUp();
        _result = await AppendValue("new-value");
    }

    [Fact] async Task should_restore_the_history() => (await Values("source")).ShouldContainOnly(Hash("current"));
    [Fact] void should_admit_governed_appends() => _result.IsSuccess.ShouldBeTrue();
}
