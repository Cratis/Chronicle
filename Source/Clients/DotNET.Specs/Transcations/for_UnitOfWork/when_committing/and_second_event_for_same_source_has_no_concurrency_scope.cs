// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class and_second_event_for_same_source_has_no_concurrency_scope : given.a_unit_of_work
{
    EventSourceId _eventSourceId;
    ConcurrencyScope _concurrencyScope;
    Causation _causation;

    protected override AppendManyResult GetAppendResult() => new()
    {
        CorrelationId = _correlationId,
    };

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _concurrencyScope = new ConcurrencyScope(42UL, _eventSourceId);
        _causation = new Causation(DateTimeOffset.UtcNow, "cause", new Dictionary<string, string>());

        _unitOfWork.AddEvent(EventSequenceId.Log, _eventSourceId, new FirstEvent(), _causation, concurrencyScope: _concurrencyScope);
        _unitOfWork.AddEvent(EventSequenceId.Log, _eventSourceId, new SecondEvent(), _causation);
    }

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_still_apply_concurrency_for_the_source() => _concurrencyScopesAppended.ContainsKey(_eventSourceId).ShouldBeTrue();
    [Fact] void should_keep_the_real_concurrency_scope() => _concurrencyScopesAppended[_eventSourceId].ShouldEqual(_concurrencyScope);

    record FirstEvent();
    record SecondEvent();
}
