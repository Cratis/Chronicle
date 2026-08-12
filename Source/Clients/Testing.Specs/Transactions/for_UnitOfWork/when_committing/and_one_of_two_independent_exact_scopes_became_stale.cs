// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.Transactions.for_UnitOfWork.when_committing;

public class and_one_of_two_independent_exact_scopes_became_stale : Specification, IDisposable
{
    EventScenario _scenario;
    UnitOfWork _unitOfWork;
    EventSourceId _target;
    bool _targetHasEvents;

    void Establish()
    {
        _scenario = new EventScenario();
        _target = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.EventLog.Append(EventSourceId.New(), new TestEvent("decision revision"));
        await _scenario.EventLog.Append(EventSourceId.New(), new MemberActivityRecorded());

        var testEventType = Defaults.Instance.EventTypes.GetEventTypeFor(typeof(TestEvent));
        var activityEventType = Defaults.Instance.EventTypes.GetEventTypeFor(typeof(MemberActivityRecorded));
        var eventStore = Substitute.For<IEventStore>();
        eventStore.GetEventSequence(EventSequenceId.Log).Returns(_scenario.EventLog);
        _unitOfWork = new(CorrelationId.New(), _ => { }, eventStore);
        _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [new(_target, new TestEvent("target"))],
            [
                new(EventSourceId.New(), new ConcurrencyScope(EventSequenceNumber.First, EventTypes: [testEventType])),
                new(EventSourceId.New(), new ConcurrencyScope(new EventSequenceNumber(1), EventTypes: [activityEventType]))
            ]);

        await _scenario.EventLog.Append(EventSourceId.New(), new MemberActivityRecorded());
        await _unitOfWork.Commit();
        _targetHasEvents = await _scenario.EventLog.HasEventsFor(_target);
    }

    [Fact] void should_reject_when_either_independent_scope_is_stale() => _unitOfWork.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_stale_scope() => _unitOfWork.GetConcurrencyViolations().ShouldNotBeEmpty();
    [Fact] void should_leave_no_target_event() => _targetHasEvents.ShouldBeFalse();

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _scenario.Dispose();
    }
}
