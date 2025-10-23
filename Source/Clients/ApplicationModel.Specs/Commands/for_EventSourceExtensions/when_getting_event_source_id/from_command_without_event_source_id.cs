// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_EventSourceExtensions.when_getting_event_source_id;

public class from_command_without_event_source_id : Specification
{
    CommandWithoutEventSourceId _command;
    EventSourceId _result;

    void Establish() => _command = new("some-data");

    void Because() => _result = _command.GetEventSourceId();

    [Fact] void should_return_unspecified() => _result.ShouldEqual(EventSourceId.Unspecified);

    record CommandWithoutEventSourceId(string Data);
}
