// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Applications.Commands.for_AppendResultExtensions.when_converting_append_result;

public class with_errors : given.all_dependencies
{
    AppendResult _appendResult;
    CommandResult _result;

    void Establish()
    {
        var errors = new[]
        {
            new AppendError("First error"),
            new AppendError("Second error")
        };

        _appendResult = AppendResult.Failed(_correlationId, errors);
    }

    void Because() => _result = _appendResult.ToCommandResult();

    [Fact] void should_return_failed_command_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_correct_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_have_exception_messages() => _result.ExceptionMessages.ShouldNotBeEmpty();
    [Fact] void should_have_two_exception_messages() => _result.ExceptionMessages.Count().ShouldEqual(2);
    [Fact] void should_include_first_error() => _result.ExceptionMessages.ShouldContain("First error");
    [Fact] void should_include_second_error() => _result.ExceptionMessages.ShouldContain("Second error");
}
