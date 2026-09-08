// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Chronicle.Services.for_CommandExecutor.when_executing;

public class and_pipeline_reports_an_exception : given.a_command_pipeline
{
    Contracts.Commands.CommandResult _result;

    void Establish() =>
        _pipeline.Execute(_command, Arg.Any<ValidationResultSeverity?>())
            .Returns(Arc.Commands.CommandResult.Error(_correlationId, new SomethingWentWrong()));

    async Task Because() => _result = await CommandExecutor.Execute(_pipeline, _command);

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_carry_the_exception_message() => _result.ExceptionMessages[0].ShouldEqual("Something went wrong");
    [Fact] void should_not_have_validation_results() => _result.ValidationResults.ShouldBeEmpty();

    class SomethingWentWrong() : Exception("Something went wrong");
}
