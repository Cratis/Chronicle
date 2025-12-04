// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Applications.Commands.for_CommandContextExtensions.when_checking_has_event_source_id;

public class without_event_source_id : given.a_command_context
{
    bool _result;

    void Because() => _result = _commandContext.HasEventSourceId();

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
