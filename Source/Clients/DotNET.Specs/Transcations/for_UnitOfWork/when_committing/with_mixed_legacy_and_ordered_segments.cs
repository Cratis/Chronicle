// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class with_mixed_legacy_and_ordered_segments : given.a_unit_of_work
{
    readonly Causation _causation = Causation.Unknown();
    object[] _expectedEvents;
    object[] _committedEvents;

    void Establish()
    {
        var firstSource = EventSourceId.New();
        var secondSource = EventSourceId.New();
        var thirdSource = EventSourceId.New();
        var first = new FirstEvent();
        var second = new SecondEvent();
        var third = new ThirdEvent();
        var fourth = new FourthEvent();
        var fifth = new FifthEvent();
        var sixth = new SixthEvent();
        var seventh = new SeventhEvent();
        var eighth = new EighthEvent();
        _expectedEvents = [first, third, second, fourth, fifth, sixth, eighth, seventh];

        _unitOfWork.AddEvent(EventSequenceId.Log, firstSource, first, _causation);
        _unitOfWork.AddEvent(EventSequenceId.Log, secondSource, second, _causation);
        _unitOfWork.AddEvent(EventSequenceId.Log, firstSource, third, _causation);
        _unitOfWork.AddEvents(EventSequenceId.Log, [new(secondSource, fourth), new(firstSource, fifth)], []);
        _unitOfWork.AddEvent(EventSequenceId.Log, thirdSource, sixth, _causation);
        _unitOfWork.AddEvent(EventSequenceId.Log, secondSource, seventh, _causation);
        _unitOfWork.AddEvent(EventSequenceId.Log, thirdSource, eighth, _causation);
    }

    async Task Because()
    {
        await _unitOfWork.Commit();
        _committedEvents = _eventsAppended.Select(_ => _.Event).ToArray();
    }

    [Fact] void should_keep_each_ordered_batch_between_legacy_source_grouped_segments() => _committedEvents.SequenceEqual(_expectedEvents).ShouldBeTrue();
    [Fact] void should_append_every_segment_once() => _eventSequence.Received(1).AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IDictionary<EventSourceId, EventSequences.Concurrency.ConcurrencyScope>>());

    record FirstEvent;
    record SecondEvent;
    record ThirdEvent;
    record FourthEvent;
    record FifthEvent;
    record SixthEvent;
    record SeventhEvent;
    record EighthEvent;
}
