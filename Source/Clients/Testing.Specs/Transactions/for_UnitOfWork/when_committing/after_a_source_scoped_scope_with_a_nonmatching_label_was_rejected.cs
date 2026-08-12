// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.Transactions.for_UnitOfWork.when_committing;

public class after_a_source_scoped_scope_with_a_nonmatching_label_was_rejected : Specification, IDisposable
{
    EventScenario _scenario;
    UnitOfWork _unitOfWork;
    EventSourceId _acceptedTarget;
    EventSourceId _rejectedTarget;
    Exception _enrollmentError;
    bool _acceptedTargetHasEvents;
    bool _rejectedTargetHasEvents;

    void Establish()
    {
        _scenario = new EventScenario();
        _acceptedTarget = EventSourceId.New();
        _rejectedTarget = EventSourceId.New();
    }

    async Task Because()
    {
        var eventStore = Substitute.For<IEventStore>();
        eventStore.GetEventSequence(EventSequenceId.Log).Returns(_scenario.EventLog);
        _unitOfWork = new(CorrelationId.New(), _ => { }, eventStore);
        _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [new(_acceptedTarget, new TestEvent("accepted enrollment"))],
            []);

        _enrollmentError = Catch.Exception(() => _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [new(_rejectedTarget, new TestEvent("rejected enrollment"))],
            [new(EventSourceId.New(), new ConcurrencyScope(EventSequenceNumber.First, _acceptedTarget))]));

        await _unitOfWork.Commit();
        _acceptedTargetHasEvents = await _scenario.EventLog.HasEventsFor(_acceptedTarget);
        _rejectedTargetHasEvents = await _scenario.EventLog.HasEventsFor(_rejectedTarget);
    }

    [Fact] void should_reject_the_mismatched_source_scope_at_enrollment() => _enrollmentError.ShouldBeOfExactType<ConcurrencyScopeEventSourceIdDoesNotMatchLabel>();
    [Fact] void should_commit_the_previously_staged_event() => _acceptedTargetHasEvents.ShouldBeTrue();
    [Fact] void should_not_partially_stage_the_rejected_event() => _rejectedTargetHasEvents.ShouldBeFalse();

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _scenario.Dispose();
    }
}
