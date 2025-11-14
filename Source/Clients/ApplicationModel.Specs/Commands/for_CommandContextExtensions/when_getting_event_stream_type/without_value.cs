// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_CommandContextExtensions.when_getting_event_stream_type;

public class without_value : given.a_command_context
{
    EventStreamType? _result;

    void Because() => _result = _commandContext.GetEventStreamType();

    [Fact] void should_return_null() => _result.ShouldBeNull();
}
