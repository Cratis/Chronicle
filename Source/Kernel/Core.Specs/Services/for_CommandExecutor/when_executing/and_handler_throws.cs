// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;

namespace Cratis.Chronicle.Services.for_CommandExecutor.when_executing;

public class and_handler_throws : given.a_command_with_a_validator
{
    CommandResult _result;

    async Task Because() => _result = await CommandExecutor.Execute(
        new TheCommand("Something"),
        _ => throw new SomethingWentWrong());

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_capture_the_exception_message() => _result.ExceptionMessages[0].ShouldEqual("Something went wrong");
    [Fact] void should_not_have_validation_results() => _result.ValidationResults.ShouldBeEmpty();

    class SomethingWentWrong() : Exception("Something went wrong");
}
