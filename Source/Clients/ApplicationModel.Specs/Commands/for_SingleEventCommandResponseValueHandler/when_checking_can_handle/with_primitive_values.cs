// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Applications.Commands.for_SingleEventCommandResponseValueHandler.when_checking_can_handle;

public class with_primitive_values : given.a_single_event_command_response_value_handler
{
    void Establish()
    {
        _eventTypes.HasFor(typeof(string)).Returns(false);
        _eventTypes.HasFor(typeof(int)).Returns(false);
        _eventTypes.HasFor(typeof(bool)).Returns(false);
        _eventTypes.HasFor(typeof(Guid)).Returns(false);
    }

    [Fact] void should_return_false_for_string() => _handler.CanHandle(_commandContext, "primitive string").ShouldBeFalse();
    [Fact] void should_return_false_for_int() => _handler.CanHandle(_commandContext, 42).ShouldBeFalse();
    [Fact] void should_return_false_for_boolean() => _handler.CanHandle(_commandContext, true).ShouldBeFalse();
    [Fact] void should_return_false_for_guid() => _handler.CanHandle(_commandContext, Guid.NewGuid()).ShouldBeFalse();
}
