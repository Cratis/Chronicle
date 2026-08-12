// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_semantically_identical_scopes_across_batches : given.a_unit_of_work
{
    EventSourceId _scopeLabel;
    ConcurrencyScope _originalScope;
    Exception _error;

    void Establish()
    {
        _scopeLabel = EventSourceId.New();
        EventType firstEventType = new("first", 1);
        EventType secondEventType = new("second", 1);
        _originalScope = new(42UL, EventStreamType: "stream", EventTypes: [firstEventType, secondEventType]);
        var equivalentScope = new ConcurrencyScope(42UL, EventStreamType: "stream", EventTypes: [secondEventType, firstEventType]);
        _unitOfWork.AddEvents(EventSequenceId.Log, [], [new(_scopeLabel, _originalScope)]);

        _error = Catch.Exception(() => _unitOfWork.AddEvents(EventSequenceId.Log, [], [new(_scopeLabel, equivalentScope)]));
    }

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_accept_the_semantically_identical_scope() => _error.ShouldBeNull();
    [Fact] void should_keep_the_original_scope_values() => _concurrencyScopesAppended[_scopeLabel].SequenceNumber.ShouldEqual(_originalScope.SequenceNumber);
    [Fact] void should_keep_the_original_scope_event_types() => _concurrencyScopesAppended[_scopeLabel].EventTypes.ToHashSet().SetEquals(_originalScope.EventTypes).ShouldBeTrue();
}
