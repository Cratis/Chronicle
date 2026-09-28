// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_staging_after_completion;

public class and_strict_unit_failed_to_commit : given.a_unit_of_work
{
    readonly object _original = new();
    Exception _commitError;
    Exception _eventError;
    Exception _batchError;
    bool _enumerated;

    void Establish()
    {
        _unitOfWork = new UnitOfWork(_correlationId, OnUnitOfWorkCompleted, _eventStore, lifecyclePolicy: UnitOfWorkLifecyclePolicy.Strict);
        _unitOfWork.AddEvent(EventSequenceId.Log, "original", _original, Causation.Unknown());
        _eventSequence.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>()).Returns(_ => Task.FromException<AppendManyResult>(new InvalidOperationException("append failed")));
    }

    async Task Because()
    {
        _commitError = await Catch.Exception(_unitOfWork.Commit);
        _eventError = Record.Exception(() => _unitOfWork.AddEvent(EventSequenceId.Log, "late", new object(), Causation.Unknown()));
        _batchError = Record.Exception(() => _unitOfWork.AddEvents(EventSequenceId.Log, LazyEvents(), []));
    }

    IEnumerable<EventForEventSourceId> LazyEvents()
    {
        _enumerated = true;
        yield return new EventForEventSourceId("late", new object(), Causation.Unknown());
    }

    [Fact] void should_propagate_commit_failure() => _commitError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_refuse_an_event() => _eventError.ShouldBeOfExactType<UnitOfWorkIsCompleted>();
    [Fact] void should_refuse_a_batch() => _batchError.ShouldBeOfExactType<UnitOfWorkIsCompleted>();
    [Fact] void should_not_enumerate_the_batch() => _enumerated.ShouldBeFalse();
    [Fact] void should_keep_original_staged_events() => _unitOfWork.GetEvents().ShouldContainOnly([_original]);
}
