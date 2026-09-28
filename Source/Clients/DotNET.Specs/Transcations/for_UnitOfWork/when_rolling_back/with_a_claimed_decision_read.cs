// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_rolling_back;

public class with_a_claimed_decision_read : given.a_unit_of_work
{
    DecisionReadCommitOwner _owner;
    Exception _rollbackError;
    Exception _wrongOwnerError;
    Exception _nullOwnerError;
    Exception _disposeError;
    bool _eventsStillStaged;
    bool _stillOpen;
    bool _callbackNotCalled;

    void Establish()
    {
        _eventStore.Name.Returns((EventStoreName)"store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"namespace");
        _owner = _unitOfWork.ClaimDecisionReadCommitOwnership();
        _unitOfWork.AddDecisionRead(new DecisionRead<object>(
            "source", null, _eventStore.Name, _eventStore.Namespace, 4, [new EventType("created", EventTypeGeneration.First)]));
        _unitOfWork.AddEvent(EventSequenceId.Log, "source", new object(), Causation.Unknown());
    }

    async Task Because()
    {
        IUnitOfWork sdkUnit = _unitOfWork;
        _rollbackError = await Record.ExceptionAsync(sdkUnit.Rollback);
        using var other = new UnitOfWork(CorrelationId.New(), _ => { }, _eventStore);
        _wrongOwnerError = await Record.ExceptionAsync(() => _unitOfWork.RollbackAsOwner(other.ClaimDecisionReadCommitOwnership()));
        _nullOwnerError = await Record.ExceptionAsync(() => _unitOfWork.RollbackAsOwner(null!));
        _disposeError = Record.Exception(_unitOfWork.Dispose);
        _eventsStillStaged = _unitOfWork.GetEvents().Any();
        _stillOpen = !_unitOfWork.IsCompleted;
        _callbackNotCalled = !_onCompletedCalled;
        await _unitOfWork.RollbackAsOwner(_owner);
    }

    [Fact] void should_refuse_direct_sdk_rollback() => _rollbackError.ShouldBeOfExactType<ProtectedUnitOfWorkRequiresOwner>();
    [Fact] void should_refuse_another_owner() => _wrongOwnerError.ShouldBeOfExactType<ProtectedUnitOfWorkRequiresOwner>();
    [Fact] void should_refuse_a_null_owner() => _nullOwnerError.ShouldBeOfExactType<ProtectedUnitOfWorkRequiresOwner>();
    [Fact] void should_refuse_disposal() => _disposeError.ShouldBeOfExactType<ProtectedUnitOfWorkRequiresOwner>();
    [Fact] void should_retain_staged_events_on_refusal() => _eventsStillStaged.ShouldBeTrue();
    [Fact] void should_leave_the_unit_open_on_refusal() => _stillOpen.ShouldBeTrue();
    [Fact] void should_not_call_completion_on_refusal() => _callbackNotCalled.ShouldBeTrue();
    [Fact] void should_clear_staged_events_on_owner_rollback() => _unitOfWork.GetEvents().ShouldBeEmpty();
    [Fact] void should_complete_on_owner_rollback() => _unitOfWork.IsCompleted.ShouldBeTrue();
    [Fact] void should_call_completion_on_owner_rollback() => _onCompletedCalled.ShouldBeTrue();
    [Fact] void should_not_append() => _eventSequence.DidNotReceiveWithAnyArgs().AppendMany(default!);
}
