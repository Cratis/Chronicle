// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.XUnit.Integration.for_EventAppendCollection;

public class when_an_append_many_with_successful_result_is_observed : given.an_event_append_collection
{
    CorrelationId _correlationId;
    EventSourceId _eventSourceId;
    object _firstEvent;
    object _secondEvent;
    Causation _causation;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _eventSourceId = EventSourceId.New();
        _firstEvent = new object();
        _secondEvent = new object();
        _causation = Causation.Unknown();
    }

    void Because()
    {
        _subject.OnNext(
        [
            MakeAppendedEvent(_correlationId, _eventSourceId, _firstEvent, [_causation], AppendResult.Success(_correlationId, new EventSequenceNumber(10))),
            MakeAppendedEvent(_correlationId, _eventSourceId, _secondEvent, [_causation], AppendResult.Success(_correlationId, new EventSequenceNumber(11)))
        ]);
    }

    [Fact] void should_collect_both_events() => _collection.All.Count.ShouldEqual(2);
    [Fact] void should_assign_the_first_sequence_number() => _collection.All[0].Result.SequenceNumber.ShouldEqual(new EventSequenceNumber(10));
    [Fact] void should_assign_the_second_sequence_number() => _collection.All[1].Result.SequenceNumber.ShouldEqual(new EventSequenceNumber(11));
    [Fact] void should_be_for_the_correct_event_source() => _collection.All.All(e => e.Event.Context.EventSourceId == _eventSourceId).ShouldBeTrue();
    [Fact] void should_carry_the_first_event() => _collection.All[0].Event.Content.ShouldEqual(_firstEvent);
    [Fact] void should_carry_the_second_event() => _collection.All[1].Event.Content.ShouldEqual(_secondEvent);
    [Fact] void should_carry_the_causation_on_each_event() => _collection.All.All(e => e.Event.Context.Causation.Contains(_causation)).ShouldBeTrue();
    [Fact] void should_not_have_constraint_violations_on_any() => _collection.All.All(e => !e.Result.HasConstraintViolations).ShouldBeTrue();
    [Fact] void should_be_successful_for_all() => _collection.All.All(e => e.Result.IsSuccess).ShouldBeTrue();
}
