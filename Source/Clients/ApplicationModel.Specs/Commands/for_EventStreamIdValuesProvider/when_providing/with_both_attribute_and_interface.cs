// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_EventStreamIdValuesProvider.when_providing;

public class with_both_attribute_and_interface : Specification
{
    EventStreamIdValuesProvider _provider;
    TestCommand _command;
    Exception _exception;

    void Establish()
    {
        _provider = new EventStreamIdValuesProvider();
        _command = new TestCommand("Quarterly");
    }

    void Because() => _exception = Catch.Exception(() => _provider.Provide(_command));

    [Fact] void should_throw_ambiguous_event_stream_id() => _exception.ShouldBeOfExactType<AmbiguousEventStreamId>();
    [Fact] void should_indicate_command_type() => _exception.Message.ShouldContain("TestCommand");

    [EventStreamId("Monthly")]
    record TestCommand(EventStreamId EventStreamId) : ICanProvideEventStreamId
    {
        public EventStreamId GetEventStreamId() => EventStreamId;
    }
}
