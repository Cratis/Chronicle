// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_failed_constraint_index_update.and_the_repair_fails;

public class and_the_backoff_has_not_elapsed : given.an_event_sequence_with_repairable_indexes
{
    async Task Establish()
    {
        _failWrites = true;
        await AppendAnEvent();
        await AppendAnEvent();
        _rangeReads = 0;
    }
    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_refuse_immediately() => _result.Errors.ShouldContain(new AppendError("ConstraintIndexUnavailable"));
    [Fact] void should_not_read_the_range_again() => _rangeReads.ShouldEqual(0);
    [Fact] void should_write_nothing_more() => _log.Events.Count.ShouldEqual(1);
}
