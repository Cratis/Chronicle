// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_failed_constraint_index_update;

public class and_a_closing_update_is_pending : given.an_event_sequence_with_repairable_indexes
{
    protected override bool Closing => true;
    async Task Establish()
    {
        _failWrites = true;
        await AppendAnEvent();
        _eventType = new("UnrelatedEvent", EventTypeGeneration.First);
    }
    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_refuse_every_event_type() => _result.Errors.ShouldContain(AppendError.ConstraintIndexUnavailable);
    [Fact] void should_store_nothing_more() => _log.Events.Count.ShouldEqual(1);
}
