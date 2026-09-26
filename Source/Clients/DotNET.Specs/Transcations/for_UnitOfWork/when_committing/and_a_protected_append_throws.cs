// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class and_a_protected_append_throws : given.a_unit_of_work
{
    readonly Exception _appendException = new InvalidOperationException("append failed");
    Exception _caught;
    bool _successOnCompletion;

    void Establish()
    {
        _eventStore.Name.Returns((EventStoreName)"store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"namespace");
        _unitOfWork.AddDecisionRead(new DecisionRead<object>("source", null, _eventStore.Name, _eventStore.Namespace, 4, [new EventType("created", 1)]));
        _unitOfWork.OnCompleted(_ => _successOnCompletion = _.IsSuccess);
        _eventSequence.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>())
            .Returns<Task<AppendManyResult>>(_ => throw _appendException);
    }

    async Task Because() => _caught = await Record.ExceptionAsync(() => _unitOfWork.CommitAsOwner(_unitOfWork.ClaimDecisionReadCommitOwnership()));

    [Fact] void should_propagate_the_exception() => _caught.ShouldEqual(_appendException);
    [Fact] void should_complete_the_unit() => _unitOfWork.IsCompleted.ShouldBeTrue();
    [Fact] void should_record_a_failed_outcome() => _unitOfWork.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_failure_to_the_completion_callback() => _successOnCompletion.ShouldBeFalse();
}
