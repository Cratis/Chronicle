// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_a_protected_absent_model_changes_before_validate_only : Specification
{
    EventStoreForTesting _store;
    EventSourceId _source;
    IUnitOfWork _unit;

    async Task Establish()
    {
        _store = new EventStoreForTesting();
        _source = EventSourceId.New();
        _unit = _store.UnitOfWorkManager.Begin(CorrelationId.New());
        _unit.AddDecisionRead(await _store.GetDecisionReads().GetDetached<SimpleModule>(_source));
        await _store.EventLog.Append(_source, new ModuleCreated("Concurrent"));
    }

    async Task Because()
    {
        var concrete = (UnitOfWork)_unit;
        await concrete.CommitAsOwner(concrete.ClaimDecisionReadCommitOwnership());
    }

    [Fact] void should_report_a_conflict() => _unit.GetDecisionConflicts().ShouldContain(_ => _.Key.Value == _source.Value);
    [Fact] void should_not_succeed() => _unit.IsSuccess.ShouldBeFalse();
    [Fact] async Task should_leave_only_the_concurrent_event_in_the_log() =>
        (await _store.EventLog.GetTailSequenceNumber()).ShouldEqual(EventSequenceNumber.First);
}
