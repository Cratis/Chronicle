// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_failed_constraint_index_update;

public class and_many_governed_appends_are_queued_behind_an_unfinished_fold : given.an_unfinished_constraint_repair
{
    readonly List<AppendResult> _refused = [];

    async Task Because()
    {
        // The non-reentrant grain executes queued turns serially, without advancing repair time.
        for (var turn = 0; turn < 25; turn++) _refused.Add(await AppendAnEvent());
        _eventType = new("Unrelated", EventTypeGeneration.First);
        _result = await AppendAnEvent();
    }

    [Fact] void should_refuse_every_governed_turn() => _refused.ShouldEachConformTo(result => result.Errors.Contains(AppendError.ConstraintIndexUnavailable));
    [Fact] void should_run_only_the_first_turns_repair() => _rangeReads.ShouldEqual(1);
    [Fact] void should_defer_all_pending_index_updates() => _claimWrites.ShouldEqual(0);
    [Fact] void should_admit_the_non_governed_turn_without_waiting_for_repair() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_only_the_non_governed_event() => _log.Events.Count.ShouldEqual(2);
}
