// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_disposing;

public class with_an_enrolled_decision_read : given.a_unit_of_work
{
    void Establish()
    {
        _eventStore.Name.Returns((EventStoreName)"store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"namespace");
        _unitOfWork.AddDecisionRead(new DecisionRead<object>(
            "source", null, _eventStore.Name, _eventStore.Namespace, 4, [new EventType("created", EventTypeGeneration.First)]));
    }

    void Because() => _unitOfWork.Dispose();

    [Fact] void should_keep_the_enrollment_visible_after_disposal() => ((IUnitOfWork)_unitOfWork).HasEnrolledDecisionReads.ShouldBeTrue();
    [Fact] void should_complete_the_unit() => _unitOfWork.IsCompleted.ShouldBeTrue();
}
