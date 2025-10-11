// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Applications.Commands.for_EventSourceExtensions.when_getting_event_source_id;

public class from_command_with_key_attribute_property : Specification
{
    CommandWithKeyAttribute _command;
    string _keyValue;
    EventSourceId _result;

    void Establish()
    {
        _keyValue = "some-key-value";
        _command = new(_keyValue);
    }

    void Because() => _result = _command.GetEventSourceId();

    [Fact] void should_return_the_key_value_as_event_source_id() => _result.ShouldEqual((EventSourceId)_keyValue);

    public class CommandWithKeyAttribute(string id)
    {
        [Key]
        public string Id { get; init; } = id;
    }
}
