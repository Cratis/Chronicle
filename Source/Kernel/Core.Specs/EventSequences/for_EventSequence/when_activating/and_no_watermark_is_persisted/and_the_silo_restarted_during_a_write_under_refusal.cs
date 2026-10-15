// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_the_silo_restarted_during_a_write_under_refusal : given.a_catching_up_sequence
{
    AppendResult _beforeRecovery;

    async Task Establish()
    {
        await TransferHistory();
        _failWrites = true;
        await Tick();
        await Reactivate();
        _failWrites = false;
    }

    async Task Because()
    {
        _beforeRecovery = await AppendAnEvent();
        await Tick();
        _result = await AppendAnEvent();
    }

    [Fact] void should_refuse_before_the_restarted_fold_repairs_the_missing_claim() => _beforeRecovery.Errors.ShouldContain(AppendError.ConstraintIndexUnavailable);
    [Fact] async Task should_restore_the_claim_from_history() => (await Values("new-holder")).ShouldContain(Hash("value"));
    [Fact] void should_admit_after_the_catch_up_finishes() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_persist_the_cleared_boundary() => _snapshots.Written!.ConstraintIndexCatchUpBelow.ShouldBeNull();
}
