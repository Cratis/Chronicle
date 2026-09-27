// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_rolling_back;

public class with_an_enrolled_decision_read : given.a_unit_of_work
{
    void Establish()
    {
        _eventStore.Name.Returns((EventStoreName)"store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"namespace");
        _unitOfWork.AddDecisionRead(new DecisionRead<object>(
            "source", null, _eventStore.Name, _eventStore.Namespace, 4, [new EventType("created", EventTypeGeneration.First)]));
    }

    async Task Because() => await _unitOfWork.Rollback();

    [Fact] void should_keep_the_enrollment_visible_after_rollback() => ((IUnitOfWork)_unitOfWork).HasEnrolledDecisionReads.ShouldBeTrue();
    [Fact] void should_not_append_after_rollback() => _eventSequence.DidNotReceiveWithAnyArgs().AppendMany(default!);
}
