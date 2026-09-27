// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_a_rejected_decision_read : given.a_unit_of_work
{
    Exception _error;

    void Establish()
    {
        _eventStore.Name.Returns((EventStoreName)"store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"namespace");
    }

    void Because() => _error = Record.Exception(() => _unitOfWork.AddDecisionRead(new DecisionRead<object>(
        "source", null, (EventStoreName)"another-store", _eventStore.Namespace, 4, [new EventType("created", EventTypeGeneration.First)])));

    [Fact] void should_reject_the_read() => _error.ShouldBeOfExactType<DecisionReadTargetMismatch>();
    [Fact] void should_not_report_enrollment() => ((IUnitOfWork)_unitOfWork).HasEnrolledDecisionReads.ShouldBeFalse();
}
