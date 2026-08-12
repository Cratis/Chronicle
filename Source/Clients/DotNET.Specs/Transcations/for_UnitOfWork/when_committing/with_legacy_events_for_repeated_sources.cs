// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class with_legacy_events_for_repeated_sources : given.a_unit_of_work
{
    readonly Causation _causation = Causation.Unknown();
    EventSourceId _firstSource;
    EventSourceId _secondSource;
    FirstEvent _firstEvent;
    SecondEvent _secondEvent;
    ThirdEvent _thirdEvent;
    object[] _committedEvents;

    void Establish()
    {
        _firstSource = EventSourceId.New();
        _secondSource = EventSourceId.New();
        _firstEvent = new();
        _secondEvent = new();
        _thirdEvent = new();
        _unitOfWork.AddEvent(EventSequenceId.Log, _firstSource, _firstEvent, _causation);
        _unitOfWork.AddEvent(EventSequenceId.Log, _secondSource, _secondEvent, _causation);
        _unitOfWork.AddEvent(EventSequenceId.Log, _firstSource, _thirdEvent, _causation);
    }

    async Task Because()
    {
        await _unitOfWork.Commit();
        _committedEvents = _eventsAppended.Select(_ => _.Event).ToArray();
    }

    [Fact] void should_commit_the_first_source_first_event_first() => _committedEvents[0].ShouldEqual(_firstEvent);
    [Fact] void should_commit_the_first_source_second_event_second() => _committedEvents[1].ShouldEqual(_thirdEvent);
    [Fact] void should_commit_the_second_source_event_last() => _committedEvents[2].ShouldEqual(_secondEvent);
    [Fact] void should_append_once() => _eventSequence.Received(1).AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IDictionary<EventSourceId, EventSequences.Concurrency.ConcurrencyScope>>());

    record FirstEvent;
    record SecondEvent;
    record ThirdEvent;
}
