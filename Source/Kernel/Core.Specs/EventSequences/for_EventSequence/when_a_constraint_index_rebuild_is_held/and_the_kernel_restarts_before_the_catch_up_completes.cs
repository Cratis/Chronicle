// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_is_held;

public class and_the_kernel_restarts_before_the_catch_up_completes : given.a_held_constraint_index_rebuild
{
    AppendResult _afterRestart;

    async Task Establish() => await HoldRebuild();

    async Task Because()
    {
        await PassLivenessCheck();
        await Reactivate();
        _eventSequence.ConstraintIndexTimeProvider = _clock;
        _afterRestart = await AppendValue("current");
        await CompleteCatchUp();
        _result = await AppendValue("new-value");
    }

    [Fact] void should_keep_refusing_after_restart() => _afterRestart.Errors.ShouldContainOnly(AppendError.ConstraintIndexUnavailable);
    [Fact] void should_admit_governed_appends_after_catch_up() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_restore_the_history() => (await Values("source")).ShouldContainOnly(Hash("current"));
}
