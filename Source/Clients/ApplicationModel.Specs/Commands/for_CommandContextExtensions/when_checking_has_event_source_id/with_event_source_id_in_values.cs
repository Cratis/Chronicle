// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands.for_CommandContextExtensions.when_checking_has_event_source_id;

public class with_event_source_id_in_values : given.a_command_context
{
    EventSourceId _eventSourceId;
    bool _result;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _commandContextValues[WellKnownCommandContextKeys.EventSourceId] = _eventSourceId;
    }

    void Because() => _result = _commandContext.HasEventSourceId();

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
