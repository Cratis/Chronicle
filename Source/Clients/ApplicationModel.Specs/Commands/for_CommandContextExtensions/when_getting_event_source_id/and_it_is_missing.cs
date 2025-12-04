// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Applications.Commands.for_CommandContextExtensions.when_getting_event_source_id;

public class and_it_is_missing : given.a_command_context
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => _commandContext.GetEventSourceId());

    [Fact] void should_throw_missing_event_source_id_in_command_context() => _exception.ShouldBeOfExactType<MissingEventSourceIdInCommandContext>();
}
