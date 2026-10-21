// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventsResultHandler.when_handling;

public class and_the_returned_events_are_sent_as_keyed_events : given.a_reactor_delivery_with_an_event_log
{
    static readonly EventStreamType _streamType = new("order-stream");
    static readonly EventStreamId _streamId = new("stream-1");
    static readonly EventSourceType _sourceType = new("order-source");
    static readonly Subject _subject = new("subject-1");

    List<EventForEventSourceId> _sent;

    async Task Because()
    {
        var values = new ReactorContextValues
        {
            [WellKnownReactorContextKeys.EventStreamType] = _streamType,
            [WellKnownReactorContextKeys.EventStreamId] = _streamId,
            [WellKnownReactorContextKeys.EventSourceType] = _sourceType,
            [WellKnownReactorContextKeys.Subject] = _subject
        };
        await new EventsResultHandler().Handle(_reactorContext with { Values = values }, _eventStore, new[] { new object() });
        _sent = [.. KeyedBatches()];
    }

    [Fact] void should_carry_the_stream_type() => _sent[0].EventStreamType.ShouldEqual(_streamType);
    [Fact] void should_carry_the_stream_id() => _sent[0].EventStreamId.ShouldEqual(_streamId);
    [Fact] void should_carry_the_source_type() => _sent[0].EventSourceType.ShouldEqual(_sourceType);
    [Fact] void should_carry_the_subject() => _sent[0].Subject.ShouldEqual(_subject);
    [Fact] void should_target_the_triggering_event_source() => _sent[0].EventSourceId.ShouldEqual(_eventSourceId);
}
