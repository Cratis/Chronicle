// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class and_events_are_staged_after_protected_commit_starts : given.a_unit_of_work
{
    TaskCompletionSource<AppendManyResult> _append;
    Exception _eventDuringCommit;
    Exception _batchDuringCommit;
    Exception _eventAfterCommit;
    Exception _batchAfterCommit;

    void Establish()
    {
        _eventStore.Name.Returns((EventStoreName)"store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"namespace");
        _unitOfWork.AddDecisionRead(new DecisionRead<object>("source", null, _eventStore.Name, _eventStore.Namespace, 4, [new EventType("created", 1)]));
        _append = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _eventSequence.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>()).Returns(_ => _append.Task);
    }

    async Task Because()
    {
        var commit = _unitOfWork.CommitAsOwner(_unitOfWork.ClaimDecisionReadCommitOwnership());
        _eventDuringCommit = Record.Exception(AddEvent);
        _batchDuringCommit = Record.Exception(AddBatch);
        _append.SetResult(AppendManyResult.Success(_correlationId, []));
        await commit;
        _eventAfterCommit = Record.Exception(AddEvent);
        _batchAfterCommit = Record.Exception(AddBatch);
    }

    void AddEvent() => _unitOfWork.AddEvent(EventSequenceId.Log, "source", new object(), Causation.Unknown());

    void AddBatch() => _unitOfWork.AddEvents(EventSequenceId.Log,
        [new EventForEventSourceId("source", new object(), Causation.Unknown())],
        []);

    [Fact] void should_refuse_an_event_during_commit() => _eventDuringCommit.ShouldBeOfExactType<ProtectedUnitOfWorkEventsAfterCompletion>();
    [Fact] void should_refuse_a_batch_during_commit() => _batchDuringCommit.ShouldBeOfExactType<ProtectedUnitOfWorkEventsAfterCompletion>();
    [Fact] void should_refuse_an_event_after_commit() => _eventAfterCommit.ShouldBeOfExactType<ProtectedUnitOfWorkEventsAfterCompletion>();
    [Fact] void should_refuse_a_batch_after_commit() => _batchAfterCommit.ShouldBeOfExactType<ProtectedUnitOfWorkEventsAfterCompletion>();
    [Fact] void should_not_stage_rejected_events() => _unitOfWork.GetEvents().ShouldBeEmpty();
}
