// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_a_conflicting_none_scope : given.a_unit_of_work
{
    EventSourceId _scopeLabel;
    ConcurrencyScope _originalScope;
    FirstEvent _firstEvent;
    Exception _error;

    void Establish()
    {
        _scopeLabel = EventSourceId.New();
        _originalScope = new(42UL, EventTypes: [new EventType("event", 1)]);
        _firstEvent = new();
        _unitOfWork.AddEvents(EventSequenceId.Log, [new(EventSourceId.New(), _firstEvent)], [new(_scopeLabel, _originalScope)]);
        _error = Catch.Exception(() => _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [new(EventSourceId.New(), new RejectedEvent())],
            [new(_scopeLabel, ConcurrencyScope.None)]));
    }

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_fail_with_the_domain_exception() => _error.ShouldBeOfExactType<ConflictingConcurrencyScopesForLabel>();
    [Fact] void should_name_the_event_source() => ((ConflictingConcurrencyScopesForLabel)_error).ScopeLabel.ShouldEqual(_scopeLabel);
    [Fact] void should_describe_both_scopes_in_words() => _error.Message.ShouldEqual(
        $"Event source '{_scopeLabel}' was given two different concurrency scopes in the same unit of work: first one that " +
        "expects no events after sequence number 42 (event types event), then one that turns the concurrency check off. " +
        $"A unit of work guards each event source with one scope. Capture the scope for '{_scopeLabel}' once and use it for every append to it.");
    [Fact] void should_not_stage_the_rejected_event() => _eventsAppended.Select(_ => _.Event).ShouldContainOnly(_firstEvent);
    [Fact] void should_keep_the_original_exact_revision() => _concurrencyScopesAppended[_scopeLabel].SequenceNumber.ShouldEqual(_originalScope.SequenceNumber);
    [Fact] void should_keep_the_original_event_type_filter() => _concurrencyScopesAppended[_scopeLabel].EventTypes.ShouldContainOnly(_originalScope.EventTypes);

    record FirstEvent;
    record RejectedEvent;
}
