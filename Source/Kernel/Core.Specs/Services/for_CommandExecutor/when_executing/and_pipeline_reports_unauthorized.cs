// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Chronicle.Services.for_CommandExecutor.when_executing;

public class and_pipeline_reports_unauthorized : given.a_command_pipeline
{
    Contracts.Commands.CommandResult _result;

    void Establish() =>
        _pipeline.Execute(_command, Arg.Any<ValidationResultSeverity?>())
            .Returns(Arc.Commands.CommandResult.Unauthorized(_correlationId, "Not allowed"));

    async Task Because() => _result = await CommandExecutor.Execute(_pipeline, _command);

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_authorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_carry_the_authorization_failure_reason() => _result.AuthorizationFailureReason.ShouldEqual("Not allowed");
}
