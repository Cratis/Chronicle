// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.Transactions.for_UnitOfWork.when_committing;

public class after_a_blank_batch_target_was_rejected : Specification, IDisposable
{
    EventScenario _scenario;
    UnitOfWork _unitOfWork;
    EventSourceId _blankTarget;
    Exception _enrollmentError;
    bool _blankTargetHasEvents;

    void Establish()
    {
        _scenario = new EventScenario();
        _blankTarget = new EventSourceId(" ");
    }

    async Task Because()
    {
        var eventStore = Substitute.For<IEventStore>();
        eventStore.GetEventSequence(EventSequenceId.Log).Returns(_scenario.EventLog);
        _unitOfWork = new(CorrelationId.New(), _ => { }, eventStore);
        _enrollmentError = Catch.Exception(() => _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [new(_blankTarget, new TestEvent("rejected target"))],
            []));

        await _unitOfWork.Commit();
        _blankTargetHasEvents = await _scenario.EventLog.HasEventsFor(_blankTarget);
    }

    [Fact] void should_reject_the_blank_target_at_enrollment() => _enrollmentError.ShouldBeOfExactType<ConcurrencyScopeLabelMustBeSpecified>();
    [Fact] void should_leave_no_event_on_the_blank_target() => _blankTargetHasEvents.ShouldBeFalse();

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _scenario.Dispose();
    }
}
