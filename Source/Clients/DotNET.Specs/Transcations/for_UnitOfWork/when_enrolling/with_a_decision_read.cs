// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_a_decision_read : given.a_unit_of_work
{
    bool _initiallyEnrolled;

    void Establish()
    {
        _eventStore.Name.Returns((EventStoreName)"store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"namespace");
        _initiallyEnrolled = ((IUnitOfWork)_unitOfWork).HasEnrolledDecisionReads;
    }

    void Because() => _unitOfWork.AddDecisionRead(new DecisionRead<object>(
        "source", null, _eventStore.Name, _eventStore.Namespace, 4, [new EventType("created", EventTypeGeneration.First)]));

    [Fact] void should_start_without_decision_enrollment() => _initiallyEnrolled.ShouldBeFalse();
    [Fact] void should_report_direct_sdk_enrollment_through_the_interface() => ((IUnitOfWork)_unitOfWork).HasEnrolledDecisionReads.ShouldBeTrue();
}
