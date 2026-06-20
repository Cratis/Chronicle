// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorInvocationResult.get_failure_details;

public class with_exception : Specification
{
    Exception _exception;
    (IEnumerable<string> Messages, string StackTrace) _result;

    void Establish()
    {
        try
        {
            throw new InvalidOperationException("outer failure", new Exception("inner failure"));
        }
        catch (InvalidOperationException ex)
        {
            _exception = ex;
        }
    }

    void Because() => _result = ReactorInvocationResult.FromException(_exception).GetFailureDetails();

    [Fact] void should_include_outer_exception_message() => _result.Messages.ShouldContain("outer failure");
    [Fact] void should_include_inner_exception_message() => _result.Messages.ShouldContain("inner failure");
    [Fact] void should_include_stack_trace() => _result.StackTrace.ShouldNotBeEmpty();
}
