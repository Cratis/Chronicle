// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_not_set_after_an_explicit_scope : given.a_unit_of_work
{
    EventSourceId _scopeLabel;
    ConcurrencyScope _originalScope;

    void Establish()
    {
        _scopeLabel = EventSourceId.New();
        _originalScope = new(42UL, EventTypes: [new EventType("event", 1)]);
        _unitOfWork.AddEvents(EventSequenceId.Log, [], [new(_scopeLabel, _originalScope)]);
        _unitOfWork.AddEvents(EventSequenceId.Log, [new(_scopeLabel, new TargetEvent())], [new(_scopeLabel, ConcurrencyScope.NotSet)]);
    }

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_keep_the_explicit_scope_revision() => _concurrencyScopesAppended[_scopeLabel].SequenceNumber.ShouldEqual(_originalScope.SequenceNumber);
    [Fact] void should_keep_the_explicit_scope_event_types() => _concurrencyScopesAppended[_scopeLabel].EventTypes.ShouldContainOnly(_originalScope.EventTypes);

    record TargetEvent;
}
