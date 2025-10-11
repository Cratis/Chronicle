// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Applications.Commands.for_EventSourceExtensions.when_checking_has_event_source_id_for_command;

public class without_event_source_id_property : Specification
{
    CommandWithoutEventSourceId _command;
    bool _result;

    void Establish() => _command = new("some-data");

    void Because() => _result = _command.HasEventSourceId();

    [Fact] void should_return_false() => _result.ShouldBeFalse();

    record CommandWithoutEventSourceId(string Data);
}
