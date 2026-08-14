// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Validation;

namespace Cratis.Chronicle.Services.for_CommandExecutor.when_executing;

public class and_command_is_invalid : given.a_command_with_a_validator
{
    CommandResult _result;
    bool _handlerInvoked;

    async Task Because() => _result = await CommandExecutor.Execute(
        new TheCommand(string.Empty),
        _ =>
        {
            _handlerInvoked = true;
            return Task.CompletedTask;
        });

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_invoke_the_handler() => _handlerInvoked.ShouldBeFalse();
    [Fact] void should_have_a_validation_result_with_the_message() => _result.ValidationResults[0].Message.ShouldEqual("Name is required.");
    [Fact] void should_have_a_validation_result_with_the_member() => _result.ValidationResults[0].Members[0].ShouldEqual(nameof(TheCommand.Name));
    [Fact] void should_have_error_severity() => _result.ValidationResults[0].Severity.ShouldEqual(ValidationResultSeverity.Error);
}
