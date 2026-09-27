// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWorkManager;

public class when_rolling_back_a_claimed_decision_read : given.a_unit_of_work_manager
{
    CorrelationId _correlationId;
    UnitOfWork _unit;
    DecisionReadCommitOwner _owner;
    Exception _error;
    bool _stillCurrent;
    bool _stillRegistered;
    bool _stillOpen;
    bool _clearedOnOwnerRollback;

    void Establish()
    {
        _eventStore.Name.Returns((EventStoreName)"store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"namespace");
        _correlationId = CorrelationId.New();
    }

    async Task Because()
    {
        _unit = (UnitOfWork)_manager.Begin(_correlationId);
        _owner = _unit.ClaimDecisionReadCommitOwnership();
        _unit.AddDecisionRead(new DecisionRead<object>(
            "source", null, _eventStore.Name, _eventStore.Namespace, 4, [new EventType("created", EventTypeGeneration.First)]));
        IUnitOfWork sdkUnit = _unit;
        _error = await Record.ExceptionAsync(sdkUnit.Rollback);
        _stillCurrent = ReferenceEquals(_manager.Current, _unit);
        _stillRegistered = _manager.TryGetFor(_correlationId, out var registered) && ReferenceEquals(registered, _unit);
        _stillOpen = !_unit.IsCompleted;
        await _unit.RollbackAsOwner(_owner);
        _clearedOnOwnerRollback = !_manager.HasCurrent && !_manager.TryGetFor(_correlationId, out _);
    }

    [Fact] void should_refuse_direct_sdk_rollback() => _error.ShouldBeOfExactType<ProtectedUnitOfWorkRequiresOwner>();
    [Fact] void should_retain_ambient_current_on_refusal() => _stillCurrent.ShouldBeTrue();
    [Fact] void should_retain_the_registered_unit_on_refusal() => _stillRegistered.ShouldBeTrue();
    [Fact] void should_leave_the_unit_open_on_refusal() => _stillOpen.ShouldBeTrue();
    [Fact] void should_clear_ambient_and_registration_on_owner_rollback() => _clearedOnOwnerRollback.ShouldBeTrue();
}
