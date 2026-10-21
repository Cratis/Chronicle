// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_the_constraint_store_fails_after_commit.for_a_unique_constraint;

public class and_the_sequence_is_reactivated : given.an_event_sequence_with_repairable_indexes
{
    async Task Establish()
    {
        _failWrites = true;
        await AppendAnEvent();
        _failWrites = false;
    }
    async Task Because()
    {
        await Reactivate();
        _eventSourceId = EventSourceId.New();
        _result = await AppendAnEvent();
    }

    [Fact] void should_repair_the_claim_before_refusing_the_duplicate() => _result.ConstraintViolations.ShouldNotBeEmpty();
    [Fact] void should_not_duplicate_the_durable_event() => _log.Events.Count.ShouldEqual(1);
    [Fact] void should_fold_after_reactivation() => _rangeReads.ShouldEqual(1);
}
