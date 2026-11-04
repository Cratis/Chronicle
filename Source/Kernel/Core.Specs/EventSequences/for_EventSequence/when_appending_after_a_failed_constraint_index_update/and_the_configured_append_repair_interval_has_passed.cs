// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_failed_constraint_index_update;

public class and_the_configured_append_repair_interval_has_passed : given.an_unfinished_constraint_repair
{
    void Establish() => _eventSequence.ConstraintIndexRepair = new() { AppendRepairInterval = TimeSpan.FromSeconds(1) };

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(2));
        _result = await AppendAnEvent();
    }

    [Fact] void should_repair_again_once_the_configured_interval_has_passed() => _rangeReads.ShouldEqual(2);
}
