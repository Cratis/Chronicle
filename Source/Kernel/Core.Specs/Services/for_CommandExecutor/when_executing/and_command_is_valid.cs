// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;

namespace Cratis.Chronicle.Services.for_CommandExecutor.when_executing;

public class and_command_is_valid : given.a_command_with_a_validator
{
    CommandResult _result;
    bool _handlerInvoked;

    async Task Because() => _result = await CommandExecutor.Execute(
        new TheCommand("Something"),
        _ =>
        {
            _handlerInvoked = true;
            return Task.CompletedTask;
        });

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_invoke_the_handler() => _handlerInvoked.ShouldBeTrue();
    [Fact] void should_have_a_correlation_id() => _result.CorrelationId.ShouldNotEqual(Guid.Empty);
    [Fact] void should_not_have_validation_results() => _result.ValidationResults.ShouldBeEmpty();
}
