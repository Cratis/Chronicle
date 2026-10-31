// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor;

public class and_a_decision_read_is_enrolled : given.a_contributed_unit
{
    Exception _error;

    void Establish()
    {
        _eventStore.Name.Returns((EventStoreName)"store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"namespace");
        _unitOfWork.AddDecisionRead(new DecisionRead<object>(_source, null, _eventStore.Name, _eventStore.Namespace, 4, [new EventType("created", EventTypeGeneration.First)]));
    }

    void Because() => _error = Catch.Exception(() => Stage());

    [Fact] void should_reject_freshness_instead_of_merging_it_into_the_decision_scope() => _error.ShouldBeOfExactType<DecisionReadConflictsWithConcurrencyScope>();
    [Fact] void should_not_stage_any_event() => _unitOfWork.GetEvents().ShouldBeEmpty();
}
