// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_a_mismatched_legacy_scope_then_an_ordered_batch : given.a_unit_of_work
{
    LegacyEvent _legacyEvent;
    Exception _error;

    void Establish()
    {
        _legacyEvent = new();
        _unitOfWork.AddEvent(
            EventSequenceId.Log,
            EventSourceId.New(),
            _legacyEvent,
            Causation.Unknown(),
            concurrencyScope: new ConcurrencyScope(42UL, EventSourceId.New()));

        _error = Catch.Exception(() => _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [new(EventSourceId.New(), new RejectedBatchEvent())],
            []));
    }

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_reject_batch_participation_with_the_mismatched_scope() => _error.ShouldBeOfExactType<ConcurrencyScopeEventSourceIdDoesNotMatchLabel>();
    [Fact] void should_not_stage_the_batch_event() => _eventsAppended.Select(_ => _.Event).ShouldContainOnly(_legacyEvent);

    record LegacyEvent;
    record RejectedBatchEvent;
}
