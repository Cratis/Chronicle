// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_an_explicit_scope_then_a_decision_read_for_its_event_source : given.a_unit_of_work
{
    Exception _error;

    void Establish()
    {
        _eventStore.Name.Returns((EventStoreName)"store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"namespace");
        _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [new("source", new FirstEvent())],
            [new("source", new ConcurrencyScope(EventSequenceNumber.BeforeFirst, "source"))]);
    }

    void Because() => _error = Catch.Exception(() => _unitOfWork.AddDecisionRead(new DecisionRead<object>(
        "source", null, _eventStore.Name, _eventStore.Namespace, 4, [new EventType("created", EventTypeGeneration.First)])));

    [Fact] void should_fail_with_the_decision_read_conflict() => _error.ShouldBeOfExactType<DecisionReadConflictsWithConcurrencyScope>();
    [Fact] void should_name_the_event_source() => ((ConflictingConcurrencyScopesForLabel)_error).ScopeLabel.ShouldEqual((EventSourceId)"source");

    record FirstEvent;
}
