// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Testing.EventSequences.for_EventScenario;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.Transactions.for_UnitOfWork.when_committing;

public class and_one_event_violates_a_constraint : Specification, IDisposable
{
    EventScenario _scenario;
    UnitOfWork _unitOfWork;
    EventSourceId _firstTarget;
    EventSourceId _secondTarget;
    AppendResult _siblingReplayResult;
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
        await _scenario.Given
            .ForEventSource(EventSourceId.New())
            .Events(new SubscriberRegistered(new("taken@cratis.io")));

        var eventStore = Substitute.For<IEventStore>();
        eventStore.GetEventSequence(EventSequenceId.Log).Returns(_scenario.EventLog);
        _unitOfWork = new(CorrelationId.New(), _ => { }, eventStore);
        _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [
                new(_firstTarget, new SubscriberRegistered(new("taken@cratis.io"))),
                new(_secondTarget, new SubscriberRegistered(new("sibling@cratis.io")))
            ],
            []);

        await _unitOfWork.Commit();

        _firstTargetHasEvents = await _scenario.EventLog.HasEventsFor(_firstTarget);
        _secondTargetHasEvents = await _scenario.EventLog.HasEventsFor(_secondTarget);
        _siblingReplayResult = await _scenario.When
            .ForEventSource(EventSourceId.New())
            .Events(new SubscriberRegistered(new("sibling@cratis.io")));
    }

    [Fact] void should_reject_the_batch() => _unitOfWork.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_constraint_violation() => _unitOfWork.GetConstraintViolations().ShouldNotBeEmpty();
    [Fact] void should_leave_no_event_on_the_first_target() => _firstTargetHasEvents.ShouldBeFalse();
    [Fact] void should_leave_no_event_on_the_second_target() => _secondTargetHasEvents.ShouldBeFalse();
    [Fact] void should_not_have_claimed_the_sibling_constraint_value() => _siblingReplayResult.ShouldBeSuccessful();

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _scenario.Dispose();
    }
}
