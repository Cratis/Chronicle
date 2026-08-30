// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Chronicle.Services.for_CommandExecutor.when_executing;

public class and_pipeline_reports_success : given.a_command_pipeline
{
    Contracts.Commands.CommandResult _result;

    void Establish() =>
        _pipeline.Execute(_command, Arg.Any<ValidationResultSeverity?>())
            .Returns(Arc.Commands.CommandResult.Success(_correlationId));

    async Task Because() => _result = await CommandExecutor.Execute(_pipeline, _command);

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_carry_the_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId.Value);
    [Fact] void should_not_have_validation_results() => _result.ValidationResults.ShouldBeEmpty();
    [Fact] async Task should_execute_the_command_through_the_pipeline() =>
        await _pipeline.Received(1).Execute(_command, Arg.Any<ValidationResultSeverity?>());
}
