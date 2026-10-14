// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_nested_reads_keep_the_earliest_boundary : Specification
{
    EventStoreForTesting _store;
    EventSourceId _source;
    UnitOfWork _unit;
    DecisionRead<SimpleModule> _first;
    DecisionRead<SimpleModule> _nested;
    Exception _error;

    async Task Establish()
    {
        _store = new EventStoreForTesting();
        _source = EventSourceId.New();
        await _store.EventLog.Append(_source, new ModuleCreated("First"));
    }

    async Task Because()
    {
        _unit = (UnitOfWork)_store.UnitOfWorkManager.Begin(CorrelationId.New());
        using var frame = _unit.EnforceReadProtection();
        var owner = _unit.ClaimDecisionReadCommitOwnership();
        _first = await _store.GetDecisionReads().GetFor<SimpleModule>(_unit, _source);
        Task competitor;
        using (ExecutionContext.SuppressFlow())
        {
            competitor = Task.Run(() => _store.EventLog.Append(_source, new ModuleCreated("Concurrent")));
        }
        await competitor;
        using (var nested = _unit.EnforceReadProtection())
        {
            _nested = await _store.GetDecisionReads().GetFor<SimpleModule>(_unit, _source);
        }
        _unit.AddEvent(EventSequenceId.Log, _source, new ModuleCreated("Planned"), Causation.Unknown());
        _error = await Catch.Exception(() => _unit.CommitAsOwner(owner));
    }

    void Destroy() => _store.Dispose();

    [Fact] void should_fold_the_first_acquisition() => _first.Instance!.Name.ShouldEqual("First");
    [Fact] void should_acquire_fresh_state_in_the_nested_flow() => _nested.Instance!.Name.ShouldEqual("Concurrent");
    [Fact] void should_complete_without_a_configuration_error() => _error.ShouldBeNull();
    [Fact] void should_keep_the_first_boundary() => _unit.GetAppendConditions().Single().After.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_conflict_despite_the_fresh_nested_value() => _unit.IsSuccess.ShouldBeFalse();
}
