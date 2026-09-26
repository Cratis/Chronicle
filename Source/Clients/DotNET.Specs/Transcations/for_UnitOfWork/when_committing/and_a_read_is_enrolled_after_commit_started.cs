// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class and_a_read_is_enrolled_after_commit_started : given.a_unit_of_work
{
    TaskCompletionSource<AppendManyResult> _append;
    Task _commit;
    Exception _error;

    void Establish()
    {
        _eventStore.Name.Returns((EventStoreName)"store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"namespace");
        _unitOfWork.AddDecisionRead(new DecisionRead<object>("source", null, _eventStore.Name, _eventStore.Namespace, 1, [new EventType("created", 1)]));
        _append = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _eventSequence.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>()).Returns(_ => _append.Task);
    }

    async Task Because()
    {
        _commit = _unitOfWork.CommitAsOwner(_unitOfWork.ClaimDecisionReadCommitOwnership());
        _error = Record.Exception(() => _unitOfWork.AddDecisionRead(new DecisionRead<object>(
            "other", null, _eventStore.Name, _eventStore.Namespace, 1, [new EventType("created", 1)])));
        _append.SetResult(AppendManyResult.Success(CorrelationId.New(), []));
        await _commit;
    }

    [Fact] void should_refuse_the_late_read() => _error.ShouldBeOfExactType<DecisionReadAfterCompletion>();
}
