// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_EventSourceExtensions.when_checking_has_event_source_id_for_command;

public class with_event_source_id_property : Specification
{
    CommandWithEventSourceId _command;
    bool _result;

    void Establish() => _command = new(EventSourceId.New());

    void Because() => _result = _command.HasEventSourceId();

    [Fact] void should_return_true() => _result.ShouldBeTrue();

    record CommandWithEventSourceId(EventSourceId EventSourceId);
}
