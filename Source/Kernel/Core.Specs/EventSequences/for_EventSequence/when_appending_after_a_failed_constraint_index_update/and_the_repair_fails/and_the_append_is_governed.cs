// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_failed_constraint_index_update.and_the_repair_fails;

public class and_the_append_is_governed : given.an_event_sequence_with_repairable_indexes
{
    async Task Establish()
    {
        _failWrites = true;
        await AppendAnEvent();
    }
    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_refuse_without_a_constraint_violation() => _result.ConstraintViolations.ShouldBeEmpty();
    [Fact] void should_report_an_unavailable_index() => _result.Errors.ShouldContain(new AppendError("ConstraintIndexUnavailable"));
    [Fact] void should_write_nothing_more() => _log.Events.Count.ShouldEqual(1);
}
