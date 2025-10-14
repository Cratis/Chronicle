// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Applications.Commands.for_EventSourceExtensions.when_checking_has_event_source_id_for_command;

public class with_key_attribute_property : Specification
{
    CommandWithKeyAttribute _command;
    bool _result;

    void Establish() => _command = new("some-id");

    void Because() => _result = _command.HasEventSourceId();

    [Fact] void should_return_true() => _result.ShouldBeTrue();

    public class CommandWithKeyAttribute(string id)
    {
        [Key]
        public string Id { get; init; } = id;
    }
}
