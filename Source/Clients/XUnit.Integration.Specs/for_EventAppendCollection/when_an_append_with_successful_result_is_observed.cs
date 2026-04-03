// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.XUnit.Integration.for_EventAppendCollection;

public class when_an_append_with_successful_result_is_observed : given.an_event_append_collection
{
    CorrelationId _correlationId;
    EventSourceId _eventSourceId;
    object _event;
    Causation _causation;
    CollectedEvent _collected;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _eventSourceId = EventSourceId.New();
        _event = new object();
        _causation = Causation.Unknown();
    }

    void Because()
    {
        var result = AppendResult.Success(_correlationId, new EventSequenceNumber(7));
        _collection.OnAppend(_correlationId, _eventSourceId, _event, ImmutableList.Create(_causation), result);
        _collected = _collection.Last;
    }

    [Fact] void should_have_the_correct_sequence_number() => _collected.SequenceNumber.ShouldEqual(new EventSequenceNumber(7));
    [Fact] void should_be_for_the_correct_event_source() => _collected.EventSourceId.ShouldEqual(_eventSourceId);
    [Fact] void should_carry_the_appended_event_object() => _collected.Event.ShouldEqual(_event);
    [Fact] void should_carry_the_causation() => _collected.CausationChain.Single().ShouldEqual(_causation);
    [Fact] void should_not_have_constraint_violations() => _collected.HasConstraintViolations.ShouldBeFalse();
    [Fact] void should_be_successful() => _collected.IsSuccess.ShouldBeTrue();
}
