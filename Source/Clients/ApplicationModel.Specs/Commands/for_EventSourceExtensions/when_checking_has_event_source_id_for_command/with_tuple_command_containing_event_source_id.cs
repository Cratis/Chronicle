// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_EventSourceExtensions.when_checking_has_event_source_id_for_command;

public class with_tuple_command_containing_event_source_id : Specification
{
    ITuple _command;
    bool _result;

    void Establish() => _command = (EventSourceId.New(), "some-data");

    void Because() => _result = _command.HasEventSourceId();

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
