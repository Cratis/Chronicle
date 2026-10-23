// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server.for_SchemaCommand;

public class when_no_action_is_supplied : Specification
{
    SchemaCommand _command;

    void Because() => _command = SchemaCommand.Parse(["--environment", "Production"]);

    [Fact] void should_leave_the_action_to_configuration() => _command.Action.ShouldEqual(SchemaAction.None);
    [Fact] void should_preserve_existing_arguments() => _command.RemainingArgs.ShouldContainOnly("--environment", "Production");
}
