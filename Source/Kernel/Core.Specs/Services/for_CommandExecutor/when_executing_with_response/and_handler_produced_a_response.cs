// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Chronicle.Services.for_CommandExecutor.when_executing_with_response;

public class and_handler_produced_a_response : given.a_command_pipeline
{
    internal record TheResponse(int Value);

    Contracts.Commands.CommandResult<string> _result;

    void Establish() =>
        _pipeline.Execute<TheResponse>(_command, Arg.Any<ValidationResultSeverity?>())
            .Returns(new Arc.Commands.CommandResult<TheResponse>(_correlationId, new TheResponse(42)));

    async Task Because() =>
        _result = await CommandExecutor.Execute<TheResponse, string>(_pipeline, _command, response => response.Value.ToString());

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_carry_the_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId.Value);
    [Fact] void should_map_the_response() => _result.Response.ShouldEqual("42");
}
