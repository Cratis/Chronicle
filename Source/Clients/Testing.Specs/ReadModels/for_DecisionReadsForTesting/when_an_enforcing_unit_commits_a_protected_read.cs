// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_an_enforcing_unit_commits_a_protected_read : Specification
{
    EventStoreForTesting _store;
    EventSourceId _source;
    UnitOfWork _unit;
    Exception _error;
    EventSequenceNumber _tail;

    async Task Establish()
    {
        _store = new EventStoreForTesting();
        _source = EventSourceId.New();
        await _store.EventLog.Append(_source, new ModuleCreated("Test"));
    }

    async Task Because()
    {
        _unit = (UnitOfWork)_store.UnitOfWorkManager.Begin(CorrelationId.New());
        using (var frame = _unit.EnforceReadProtection())
        {
            var owner = _unit.ClaimDecisionReadCommitOwnership();
            await _store.GetDecisionReads().GetFor<SimpleModule>(_unit, _source);
            _unit.AddEvent(EventSequenceId.Log, _source, new ModuleCreated("Changed"), Causation.Unknown());
            _error = await Catch.Exception(() => _unit.CommitAsOwner(owner));
        }
        _tail = await _store.EventLog.GetTailSequenceNumber();
    }

    void Destroy() => _store.Dispose();

    [Fact] void should_commit_through_the_trusted_default_strategy() => _error.ShouldBeNull();
    [Fact] void should_write_the_staged_event() => _tail.ShouldEqual((EventSequenceNumber)1UL);
    [Fact] void should_expose_the_read_condition() => _unit.GetAppendConditions().Single().Origins.ShouldContain(AppendConditionOrigin.ProtectedRead);
}
