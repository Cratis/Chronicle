// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors.SideEffects;

namespace Cratis.Chronicle.Reactors.for_ReactorInvocationResult;

public class when_created_from_a_side_effect_failure : Specification
{
    ReactorInvocationResult _result;

    void Because()
    {
        var sideEffectFailure = new ReactorSideEffectFailure(
            [new AppendFailure([new ReactorConstraintViolation("SomeEvent", "not unique")], false, [], [])]);

        _result = ReactorInvocationResult.FromSideEffectFailure(sideEffectFailure);
    }

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_be_failed() => _result.IsFailed.ShouldBeTrue();
    [Fact] void should_carry_the_side_effect_failure() => _result.SideEffectFailure.ShouldNotBeNull();
}
