// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_is_held;

public class and_the_release_cannot_be_persisted : given.a_held_constraint_index_rebuild
{
    AppendResult _duringFailure;

    async Task Establish()
    {
        await HoldRebuild();
        _snapshots.BeforeWrite = () => Task.FromException(new given.SimulatedStateWriteError());
    }

    async Task Because()
    {
        await PassLivenessCheck();
        _duringFailure = await AppendValue("current");
        _snapshots.BeforeWrite = () => Task.CompletedTask;
        await PassLivenessCheck();
        await CompleteCatchUp();
        _result = await AppendValue("new-value");
    }

    [Fact] void should_keep_refusing_after_failed_persistence() => _duringFailure.Errors.ShouldContainOnly(AppendError.ConstraintIndexUnavailable);
    [Fact] void should_release_after_retry() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_restore_history_after_retry() => (await Values("source")).ShouldContainOnly(Hash("current"));
}
