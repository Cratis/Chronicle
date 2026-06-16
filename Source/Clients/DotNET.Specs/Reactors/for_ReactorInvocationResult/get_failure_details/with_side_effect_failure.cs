// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors.SideEffects;

namespace Cratis.Chronicle.Reactors.for_ReactorInvocationResult.get_failure_details;

public class with_side_effect_failure : Specification
{
    (IEnumerable<string> Messages, string StackTrace) _result;

    void Because()
    {
        var sideEffectFailure = new ReactorSideEffectFailure(
            [new AppendFailure([new ReactorConstraintViolation("SomeEvent", "not unique")], false, [])]);

        _result = ReactorInvocationResult.FromSideEffectFailure(sideEffectFailure).GetFailureDetails();
    }

    [Fact] void should_include_side_effect_messages() => _result.Messages.ShouldContainOnly("Append failure 1: Constraint violation for event type 'SomeEvent': not unique");
    [Fact] void should_not_include_stack_trace() => _result.StackTrace.ShouldBeEmpty();
}
