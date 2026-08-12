// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_an_ordered_batch_then_a_mismatched_legacy_scope : given.a_unit_of_work
{
    BatchEvent _batchEvent;
    Exception _error;

    void Establish()
    {
        _batchEvent = new();
        _unitOfWork.AddEvents(EventSequenceId.Log, [new(EventSourceId.New(), _batchEvent)], []);
        _error = Catch.Exception(() => _unitOfWork.AddEvent(
            EventSequenceId.Log,
            EventSourceId.New(),
            new RejectedLegacyEvent(),
            Causation.Unknown(),
            concurrencyScope: new ConcurrencyScope(42UL, EventSourceId.New())));
    }

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_reject_the_mismatched_legacy_scope() => _error.ShouldBeOfExactType<ConcurrencyScopeEventSourceIdDoesNotMatchLabel>();
    [Fact] void should_not_stage_the_legacy_event() => _eventsAppended.Select(_ => _.Event).ShouldContainOnly(_batchEvent);

    record BatchEvent;
    record RejectedLegacyEvent;
}
