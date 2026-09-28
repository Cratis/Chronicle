// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_rolling_back;

public class with_an_unclaimed_decision_read : given.a_unit_of_work
{
    Exception _error;
    bool _stillOpen;
    bool _eventsStillStaged;

    async Task Because()
    {
        _unitOfWork.AddDecisionRead(new DecisionRead<object>(
            "source", null, _eventStore.Name, _eventStore.Namespace, 4, [new EventType("created", EventTypeGeneration.First)]));
        _unitOfWork.AddEvent(EventSequenceId.Log, "source", new object(), Cratis.Chronicle.Auditing.Causation.Unknown());
        using var other = new UnitOfWork(CorrelationId.New(), _ => { }, _eventStore);
        _error = await Record.ExceptionAsync(() => _unitOfWork.RollbackAsOwner(other.ClaimDecisionReadCommitOwnership()));
        _stillOpen = !_unitOfWork.IsCompleted;
        _eventsStillStaged = _unitOfWork.GetEvents().Any();
    }

    [Fact] void should_refuse_without_a_claim() => _error.ShouldBeOfExactType<ProtectedUnitOfWorkRequiresOwner>();
    [Fact] void should_not_complete() => _stillOpen.ShouldBeTrue();
    [Fact] void should_keep_staged_events() => _eventsStillStaged.ShouldBeTrue();
    [Fact] void should_not_notify_completion() => _onCompletedCalled.ShouldBeFalse();
}
