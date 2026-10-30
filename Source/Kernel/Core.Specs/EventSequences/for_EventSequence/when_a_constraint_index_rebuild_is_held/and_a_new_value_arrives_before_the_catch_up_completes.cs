// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_is_held;

public class and_a_new_value_arrives_before_the_catch_up_completes : given.a_held_constraint_index_rebuild
{
    AppendResult _beforeCatchUp;
    bool _refusalPersisted;

    async Task Establish() => await HoldRebuild();

    async Task Because()
    {
        await PassLivenessCheck();
        _refusalPersisted = _snapshots.Written.ConstraintIndexCatchUpRefusing?.Contains(_uniqueDefinition.Name) == true;
        _beforeCatchUp = await AppendValue("new-value");
        await CompleteCatchUp();
        _result = await AppendValue("new-value");
    }

    [Fact] void should_persist_a_separate_catch_up_refusal() => _refusalPersisted.ShouldBeTrue();
    [Fact] void should_refuse_even_a_new_value_until_catch_up() => _beforeCatchUp.Errors.ShouldContainOnly(AppendError.ConstraintIndexUnavailable);
    [Fact] void should_clear_the_catch_up_refusal_after_restoring_history() => _snapshots.Written.ConstraintIndexCatchUpRefusing.ShouldBeNull();
    [Fact] void should_admit_the_value_after_catch_up() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_restore_the_history() => (await Values("source")).ShouldContainOnly(Hash("current"));
}
