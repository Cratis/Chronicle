// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_EventStreamIdValuesProvider.when_providing;

public class with_interface_implementation : Specification
{
    EventStreamIdValuesProvider _provider;
    CommandContextValues _result;
    TestCommand _command;
    EventStreamId _eventStreamId;

    void Establish()
    {
        _provider = new EventStreamIdValuesProvider();
        _eventStreamId = "Yearly";
        _command = new TestCommand(_eventStreamId);
    }

    void Because() => _result = _provider.Provide(_command);

    [Fact] void should_return_event_stream_id_value() => _result.ContainsKey(WellKnownCommandContextKeys.EventStreamId).ShouldBeTrue();
    [Fact] void should_have_correct_event_stream_id() => ((EventStreamId)_result[WellKnownCommandContextKeys.EventStreamId]).ShouldEqual(_eventStreamId);

    record TestCommand(EventStreamId EventStreamId) : ICanProvideEventStreamId
    {
        public EventStreamId GetEventStreamId() => EventStreamId;
    }
}
