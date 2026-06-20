// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorInvocationResult.get_failure_details;

public class with_success : Specification
{
    (IEnumerable<string> Messages, string StackTrace) _result;

    void Because() => _result = ReactorInvocationResult.Success().GetFailureDetails();

    [Fact] void should_have_no_messages() => _result.Messages.ShouldBeEmpty();
    [Fact] void should_have_no_stack_trace() => _result.StackTrace.ShouldBeEmpty();
}
