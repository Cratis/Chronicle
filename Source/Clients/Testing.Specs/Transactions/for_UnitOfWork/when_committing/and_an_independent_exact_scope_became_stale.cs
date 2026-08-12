// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.Transactions.for_UnitOfWork.when_committing;

public class and_an_independent_exact_scope_became_stale : Specification, IDisposable
{
    EventScenario _scenario;
    UnitOfWork _unitOfWork;
    EventSourceId _firstTarget;
    EventSourceId _secondTarget;
    bool _firstTargetHasEvents;
    bool _secondTargetHasEvents;

    void Establish()
    {
        _scenario = new EventScenario();
        _firstTarget = EventSourceId.New();
        _secondTarget = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.EventLog.Append(EventSourceId.New(), new TestEvent("decision revision"));

        var eventType = Defaults.Instance.EventTypes.GetEventTypeFor(typeof(TestEvent));
        var exactDecisionScope = new ConcurrencyScope(EventSequenceNumber.First, EventTypes: [eventType]);
        var scopeLabel = EventSourceId.New();
        var eventStore = Substitute.For<IEventStore>();
        eventStore.GetEventSequence(EventSequenceId.Log).Returns(_scenario.EventLog);
        _unitOfWork = new(CorrelationId.New(), _ => { }, eventStore);
        _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [
                new(_firstTarget, new TestEvent("first target")),
                new(_secondTarget, new TestEvent("second target"))
            ],
            [new(scopeLabel, exactDecisionScope)]);

        await _scenario.EventLog.Append(EventSourceId.New(), new TestEvent("interference"));
        await _unitOfWork.Commit();

        _firstTargetHasEvents = await _scenario.EventLog.HasEventsFor(_firstTarget);
        _secondTargetHasEvents = await _scenario.EventLog.HasEventsFor(_secondTarget);
    }

    [Fact] void should_reject_the_stale_decision() => _unitOfWork.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_concurrency_violation() => _unitOfWork.GetConcurrencyViolations().ShouldNotBeEmpty();
    [Fact] void should_leave_no_event_on_the_first_target() => _firstTargetHasEvents.ShouldBeFalse();
    [Fact] void should_leave_no_event_on_the_second_target() => _secondTargetHasEvents.ShouldBeFalse();

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _scenario.Dispose();
    }
}
