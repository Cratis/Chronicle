// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.Transactions.for_UnitOfWork.when_committing;

public class after_a_strict_add_event_scope_filter_was_mutated : Specification, IDisposable
{
    EventScenario _scenario;
    UnitOfWork _unitOfWork;
    EventSourceId _orderedTarget;
    EventSourceId _legacyTarget;
    bool _orderedTargetHasEvents;
    bool _legacyTargetHasEvents;

    void Establish()
    {
        _scenario = new EventScenario();
        _orderedTarget = EventSourceId.New();
        _legacyTarget = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.EventLog.Append(EventSourceId.New(), new TestEvent("decision revision"));
        await _scenario.EventLog.Append(EventSourceId.New(), new MemberActivityRecorded());

        var testEventType = Defaults.Instance.EventTypes.GetEventTypeFor(typeof(TestEvent));
        var activityEventType = Defaults.Instance.EventTypes.GetEventTypeFor(typeof(MemberActivityRecorded));
        List<EventType> eventTypes = [testEventType, activityEventType];
        var eventStore = Substitute.For<IEventStore>();
        eventStore.GetEventSequence(EventSequenceId.Log).Returns(_scenario.EventLog);
        _unitOfWork = new(CorrelationId.New(), _ => { }, eventStore);
        _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [new(_orderedTarget, new TestEvent("ordered target"))],
            []);
        _unitOfWork.AddEvent(
            EventSequenceId.Log,
            _legacyTarget,
            new TestEvent("legacy target"),
            Causation.Unknown(),
            concurrencyScope: new ConcurrencyScope(new EventSequenceNumber(1), EventTypes: eventTypes));

        eventTypes.Clear();
        eventTypes.Add(activityEventType);
        await _scenario.EventLog.Append(EventSourceId.New(), new TestEvent("interference"));
        await _unitOfWork.Commit();

        _orderedTargetHasEvents = await _scenario.EventLog.HasEventsFor(_orderedTarget);
        _legacyTargetHasEvents = await _scenario.EventLog.HasEventsFor(_legacyTarget);
    }

    [Fact] void should_reject_the_stale_snapshotted_scope() => _unitOfWork.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_concurrency_violation() => _unitOfWork.GetConcurrencyViolations().ShouldNotBeEmpty();
    [Fact] void should_leave_no_event_on_the_ordered_target() => _orderedTargetHasEvents.ShouldBeFalse();
    [Fact] void should_leave_no_event_on_the_legacy_target() => _legacyTargetHasEvents.ShouldBeFalse();

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _scenario.Dispose();
    }
}
