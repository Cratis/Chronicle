// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_adding_an_event_with_origin;

public class decision_read : given.a_unit_of_work
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => _unitOfWork.AddEventWithOptions(EventSequenceId.Log, "source", new object(), Causation.Unknown(), new() { Origin = ConcurrencyScopeOrigin.DecisionRead }));

    [Fact] void should_reject_the_reserved_origin() => _exception.ShouldBeOfExactType<ConcurrencyScopeOriginNotValidForEvents>();
    [Fact] void should_not_stage_events() => _unitOfWork.GetEvents().ShouldBeEmpty();
}
