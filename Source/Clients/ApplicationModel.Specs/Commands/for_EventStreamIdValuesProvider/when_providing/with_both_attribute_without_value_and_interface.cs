// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_EventStreamIdValuesProvider.when_providing;

public class with_both_attribute_without_value_and_interface : Specification
{
    EventStreamIdValuesProvider _provider;
    CommandContextValues _result;
    TestCommand _command;

    void Establish()
    {
        _provider = new EventStreamIdValuesProvider();
        _command = new TestCommand("Quarterly");
    }

    void Because() => _result = _provider.Provide(_command);

    [Fact] void should_return_event_stream_id_value() => _result.ContainsKey(WellKnownCommandContextKeys.EventStreamId).ShouldBeTrue();
    [Fact] void should_have_event_stream_id_from_interface() => ((EventStreamId)_result[WellKnownCommandContextKeys.EventStreamId]).Value.ShouldEqual("Quarterly");

    [EventStreamId]
    record TestCommand(EventStreamId EventStreamId) : ICanProvideEventStreamId
    {
        public EventStreamId GetEventStreamId() => EventStreamId;
    }
}
