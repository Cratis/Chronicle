// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.Transactions.for_UnitOfWork.when_committing;

public class after_a_conflicting_scope_was_rejected : Specification, IDisposable
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
        await _scenario.EventLog.Append(EventSourceId.New(), new TestEvent("decision revision"));

        var eventType = Defaults.Instance.EventTypes.GetEventTypeFor(typeof(TestEvent));
        var scopeLabel = EventSourceId.New();
        var exactScope = new ConcurrencyScope(EventSequenceNumber.First, EventTypes: [eventType]);
        var eventStore = Substitute.For<IEventStore>();
        eventStore.GetEventSequence(EventSequenceId.Log).Returns(_scenario.EventLog);
        _unitOfWork = new(CorrelationId.New(), _ => { }, eventStore);
        _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [new(_acceptedTarget, new TestEvent("accepted enrollment"))],
            [new(scopeLabel, exactScope)]);

        _enrollmentError = Catch.Exception(() => _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [new(_rejectedTarget, new TestEvent("rejected enrollment"))],
            [new(scopeLabel, ConcurrencyScope.None)]));

        await _scenario.EventLog.Append(EventSourceId.New(), new TestEvent("interference"));
        await _unitOfWork.Commit();
        _acceptedTargetHasEvents = await _scenario.EventLog.HasEventsFor(_acceptedTarget);
        _rejectedTargetHasEvents = await _scenario.EventLog.HasEventsFor(_rejectedTarget);
    }

    [Fact] void should_reject_the_conflicting_scope_at_enrollment() => _enrollmentError.ShouldBeOfExactType<ConflictingConcurrencyScopesForLabel>();
    [Fact] void should_keep_the_original_exact_scope_effective() => _unitOfWork.GetConcurrencyViolations().ShouldNotBeEmpty();
    [Fact] void should_not_append_the_first_staged_event_after_its_scope_became_stale() => _acceptedTargetHasEvents.ShouldBeFalse();
    [Fact] void should_not_partially_stage_the_rejected_event() => _rejectedTargetHasEvents.ShouldBeFalse();

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _scenario.Dispose();
    }
}
