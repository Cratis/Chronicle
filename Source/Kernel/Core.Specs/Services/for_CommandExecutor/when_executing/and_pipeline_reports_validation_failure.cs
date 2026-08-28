// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Chronicle.Services.for_CommandExecutor.when_executing;

public class and_pipeline_reports_validation_failure : given.a_command_pipeline
{
    Contracts.Commands.CommandResult _result;

    void Establish()
    {
        var pipelineResult = Arc.Commands.CommandResult.Success(_correlationId);
        pipelineResult.ValidationResults =
        [
            new ValidationResult(ValidationResultSeverity.Error, "Name is required.", [nameof(TheCommand.Name)], null!)
        ];

        _pipeline.Execute(_command, Arg.Any<ValidationResultSeverity?>()).Returns(pipelineResult);
    }

    async Task Because() => _result = await CommandExecutor.Execute(_pipeline, _command);

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_carry_the_message() => _result.ValidationResults[0].Message.ShouldEqual("Name is required.");
    [Fact] void should_carry_the_member() => _result.ValidationResults[0].Members[0].ShouldEqual(nameof(TheCommand.Name));
    [Fact] void should_carry_the_error_severity() => _result.ValidationResults[0].Severity.ShouldEqual(Contracts.Validation.ValidationResultSeverity.Error);
}
