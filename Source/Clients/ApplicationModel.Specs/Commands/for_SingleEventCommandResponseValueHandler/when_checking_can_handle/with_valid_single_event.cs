// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Applications.Commands.for_SingleEventCommandResponseValueHandler.when_checking_can_handle;

public class with_valid_single_event : given.a_single_event_command_response_value_handler
{
    TestEvent _event;
    bool _result;

    void Establish()
    {
        _event = new TestEvent("Test Event");
        _eventTypes.HasFor(typeof(TestEvent)).Returns(true);
    }

    void Because() => _result = _handler.CanHandle(_commandContext, _event);

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
