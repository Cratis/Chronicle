// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_failed_constraint_index_update;

public class and_the_last_append_turn_repair_ended_less_than_5_seconds_ago : given.an_unfinished_constraint_repair
{
    void Establish() => _complianceManager.ClearReceivedCalls();

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(2));
        _result = await AppendAnEvent();
    }

    [Fact] void should_refuse_without_a_second_repair_inside_the_five_second_cadence() => _result.Errors.ShouldContain(AppendError.ConstraintIndexUnavailable);
    [Fact] void should_not_read_history_again() => _rangeReads.ShouldEqual(1);
    [Fact] void should_not_create_compliance_keys() => _complianceManager.ReceivedCalls().ShouldBeEmpty();
}
