// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_a_blank_scope_label : given.a_unit_of_work
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _unitOfWork.AddEvents(
        EventSequenceId.Log,
        [new(EventSourceId.New(), new RejectedEvent())],
        [new(new EventSourceId("\t"), ConcurrencyScope.None)]));

    [Fact] void should_fail_with_the_domain_exception() => _error.ShouldBeOfExactType<ConcurrencyScopeLabelMustBeSpecified>();
    [Fact] void should_not_stage_the_event() => _unitOfWork.GetEvents().ShouldBeEmpty();
    [Fact] void should_not_bind_the_event_sequence() => _eventStore.DidNotReceive().GetEventSequence(EventSequenceId.Log);

    record RejectedEvent;
}
