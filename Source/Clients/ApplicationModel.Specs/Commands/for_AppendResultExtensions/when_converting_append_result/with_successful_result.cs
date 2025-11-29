// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Applications.Commands.for_AppendResultExtensions.when_converting_append_result;

public class with_successful_result : given.all_dependencies
{
    AppendResult _appendResult;
    CommandResult _result;

    void Establish()
    {
        _appendResult = AppendResult.Success(_correlationId, new EventSequenceNumber(42));
    }

    void Because() => _result = _appendResult.ToCommandResult();

    [Fact] void should_return_successful_command_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_correct_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}
