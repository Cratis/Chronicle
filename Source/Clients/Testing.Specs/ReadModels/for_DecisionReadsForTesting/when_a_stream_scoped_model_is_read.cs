// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_a_stream_scoped_model_is_read : Specification
{
    EventStoreForTesting _store;
    EventSourceId _source;
    DecisionRead<StreamDecisionState> _read;
    IUnitOfWork _unit;

    async Task Establish()
    {
        _store = new EventStoreForTesting();
        _source = EventSourceId.New();
        await _store.EventLog.Append(_source, new ModuleCreated("Selected"), "modules", "selected");
        await _store.EventLog.Append(_source, new ModuleCreated("Other stream"), "modules", "other");
        await _store.EventLog.Append(EventSourceId.New(), new ModuleCreated("Other source"), "modules", "selected");
    }

    async Task Because() => _read = await _store.GetDecisionReads().GetDetached<StreamDecisionState>(_source, "modules", "selected");

    [Fact] void should_fold_only_the_selected_stream() => _read.Instance!.Name.ShouldEqual("Selected");
    [Fact] void should_use_the_stream_instance_key() => _read.Instance!.Id.ShouldEqual("selected");
    [Fact] async Task should_allow_a_concurrent_append_on_another_stream()
    {
        await _store.EventLog.Append(_source, new ModuleCreated("Concurrent other"), "modules", "other");
        _unit = _store.UnitOfWorkManager.Begin(CorrelationId.New());
        _unit.AddDecisionRead(_read);
        var concrete = (UnitOfWork)_unit;
        await concrete.CommitAsOwner(concrete.ClaimDecisionReadCommitOwnership());
        _unit.IsSuccess.ShouldBeTrue();
    }
    [Fact] async Task should_conflict_with_a_concurrent_append_on_the_same_stream()
    {
        await _store.EventLog.Append(_source, new ModuleCreated("Concurrent selected"), "modules", "selected");
        _unit = _store.UnitOfWorkManager.Begin(CorrelationId.New());
        _unit.AddDecisionRead(_read);
        var concrete = (UnitOfWork)_unit;
        await concrete.CommitAsOwner(concrete.ClaimDecisionReadCommitOwnership());
        _unit.IsSuccess.ShouldBeFalse();
        _unit.GetDecisionConflicts().ShouldContain(_ => _.Key.Value == "selected");
    }
}
