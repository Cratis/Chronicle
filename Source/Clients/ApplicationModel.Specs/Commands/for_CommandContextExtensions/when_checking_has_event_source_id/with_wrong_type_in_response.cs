// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Applications.Commands.for_CommandContextExtensions.when_checking_has_event_source_id;

public class with_wrong_type_in_response : given.a_command_context
{
    bool _result;

    void Establish()
    {
        _commandContext = _commandContext with { Response = "not-an-event-source-id" };
    }

    void Because() => _result = _commandContext.HasEventSourceId();

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
