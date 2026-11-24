// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.Namespaces;

namespace Cratis.Chronicle.Services.for_CommandExecutor.when_executing_with_response;

public class and_namespace_recovery_is_required : given.a_command_pipeline
{
    Contracts.Commands.CommandResult<string> _result;
    bool _responseMapped;

    void Establish()
    {
        var pipelineResult = new Arc.Commands.CommandResult<NamespaceRemovalStatus>(_correlationId, null)
        {
            ExceptionMessages = ["NamespaceRecoveryRequired"],
            ExceptionStackTrace = string.Empty
        };
        _pipeline.Execute<NamespaceRemovalStatus>(_command, Arg.Any<ValidationResultSeverity?>()).Returns(pipelineResult);
    }

    async Task Because() => _result = await CommandExecutor.Execute<NamespaceRemovalStatus, string>(_pipeline, _command, response =>
    {
        _responseMapped = true;
        return response.Status;
    });

    [Fact] void should_not_report_success() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_the_literal_code() => _result.ExceptionMessages.ShouldEqual<string>(["NamespaceRecoveryRequired"]);
    [Fact] void should_preserve_the_correlation() => _result.CorrelationId.ShouldEqual(_correlationId.Value);
    [Fact] void should_not_leak_a_stack() => _result.ExceptionStackTrace.ShouldBeEmpty();
    [Fact] void should_not_map_a_nonexistent_response() => _responseMapped.ShouldBeFalse();
}
