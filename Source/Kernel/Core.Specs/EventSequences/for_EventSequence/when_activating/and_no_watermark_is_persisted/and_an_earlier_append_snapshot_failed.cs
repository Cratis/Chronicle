// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_an_earlier_append_snapshot_failed : given.a_catching_up_sequence
{
    AppendResult _append;
    int _snapshotsAttempted;

    async Task Establish()
    {
        await TransferHistory();
        _snapshots.BeforeWrite = () => ++_snapshotsAttempted == 1
            ? Task.FromException(new given.SimulatedStateWriteError())
            : Task.CompletedTask;
        _append = await AppendAnEvent();
    }

    Task Because() => Tick();

    [Fact] void should_exercise_a_failed_snapshot_after_a_durable_append() => _append.IsSuccess.ShouldBeTrue();
    [Fact] void should_persist_a_new_refusal_in_the_same_turn() => _snapshots.Snapshots.ShouldContain(snapshot => snapshot.ConstraintIndexCatchUpRefusing != null);
    [Fact] async Task should_repair_the_historical_claim() => (await Values("new-holder")).ShouldContainOnly(Hash("value"));
    [Fact] void should_finish_the_catch_up_without_an_extra_failed_turn() => _stateStorage.State.ConstraintIndexCatchUpBelow.ShouldBeNull();
    [Fact] void should_clear_the_durable_refusal_after_the_repair() => _snapshots.Written!.ConstraintIndexCatchUpRefusing.ShouldBeNull();
}
