// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_SingleEventCommandResponseValueHandler.when_handling;

public class valid_single_event : given.a_single_event_command_response_value_handler
{
    TestEvent _event;
    CommandResult _result;

    void Establish()
    {
        _event = new TestEvent("Single Event");
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _event);

    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_event_to_event_log() => _eventLog.Received(1).Append(Arg.Any<EventSourceId>(), _event, Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>());
    [Fact] void should_append_with_correct_event_source_id() => _eventLog.Received().Append(((TestCommand)_commandContext.Command).EventSourceId, _event, Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>());
    [Fact] void should_return_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}
