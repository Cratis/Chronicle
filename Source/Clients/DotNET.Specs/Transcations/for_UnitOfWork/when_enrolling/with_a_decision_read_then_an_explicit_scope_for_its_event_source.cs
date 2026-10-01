// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_a_decision_read_then_an_explicit_scope_for_its_event_source : given.a_unit_of_work
{
    Exception _error;

    void Establish()
    {
        _eventStore.Name.Returns((EventStoreName)"store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"namespace");
        _unitOfWork.AddDecisionRead(new DecisionRead<object>(
            "source", null, _eventStore.Name, _eventStore.Namespace, 4, [new EventType("created", EventTypeGeneration.First)]));
    }

    void Because() => _error = Catch.Exception(() => _unitOfWork.AddEvents(
        EventSequenceId.Log,
        [new("source", new RejectedEvent())],
        [new("source", new ConcurrencyScope(EventSequenceNumber.BeforeFirst, "source"))]));

    [Fact] void should_fail_with_the_decision_read_conflict() => _error.ShouldBeOfExactType<DecisionReadConflictsWithConcurrencyScope>();
    [Fact] void should_still_be_a_conflicting_scope() => (_error is ConflictingConcurrencyScopesForLabel).ShouldBeTrue();
    [Fact] void should_say_what_to_do() => _error.Message.ShouldEqual(
        "Event source 'source' is both read as a decision read and given an explicit concurrency scope in the same unit of work. " +
        "The decision read already guards 'source' against events appended after it was read. " +
        "Remove the explicit concurrency scope for 'source' (for example from EventsWithConcurrencyScopes or an append's concurrency scope), or read it without a decision read.");
    [Fact] void should_not_dump_the_internal_scope() => _error.Message.ShouldNotContain("ConcurrencyScope {");

    record RejectedEvent;
}
