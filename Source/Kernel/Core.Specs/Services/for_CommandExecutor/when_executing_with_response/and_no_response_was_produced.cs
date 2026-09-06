// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Chronicle.Services.for_CommandExecutor.when_executing_with_response;

public class and_no_response_was_produced : given.a_command_pipeline
{
    internal record TheResponse(int Value);

    Contracts.Commands.CommandResult<string> _result;
    bool _mapperInvoked;

    void Establish()
    {
        var pipelineResult = new Arc.Commands.CommandResult<TheResponse>(_correlationId, null)
        {
            ValidationResults =
            [
                new ValidationResult(ValidationResultSeverity.Error, "Name is required.", [nameof(TheCommand.Name)], null!)
            ]
        };
        _pipeline.Execute<TheResponse>(_command, Arg.Any<ValidationResultSeverity?>()).Returns(pipelineResult);
    }

    async Task Because() =>
        _result = await CommandExecutor.Execute<TheResponse, string>(_pipeline, _command, response =>
        {
            _mapperInvoked = true;
            return response.Value.ToString();
        });

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_map_a_response() => _mapperInvoked.ShouldBeFalse();
    [Fact] void should_not_have_a_response() => _result.Response.ShouldBeNull();
    [Fact] void should_carry_the_validation_result() => _result.ValidationResults[0].Message.ShouldEqual("Name is required.");
}
