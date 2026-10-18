// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_the_constraint_store_fails_after_commit.for_a_closing_constraint;

public class and_the_sequence_is_reactivated : given.an_event_sequence_with_repairable_indexes
{
    protected override bool Closing => true;
    async Task Establish()
    {
        _failWrites = true;
        await AppendAnEvent();
        _failWrites = false;
    }
    async Task Because()
    {
        await Reactivate();
        _result = await AppendAnEvent();
    }

    [Fact] void should_repair_the_closure_before_refusing_the_append() => _result.ConstraintViolations.ShouldNotBeEmpty();
    [Fact] void should_keep_the_original_closing_event_only() => _log.Events.Count.ShouldEqual(1);
    [Fact] void should_fold_after_reactivation() => _rangeReads.ShouldEqual(1);
}
