// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectFailure.get_messages;

public class with_multiple_append_failures : Specification
{
    string[] _result;

    void Because()
    {
        var failure = new ReactorSideEffectFailure(
            [
                new AppendFailure([new ReactorConstraintViolation("FirstEvent", "first failed")], false, []),
                new AppendFailure([], true, ["second failed"])
            ]);

        _result = failure.GetMessages().ToArray();
    }

    [Fact] void should_have_all_failure_messages() => _result.Length.ShouldEqual(3);
    [Fact] void should_include_first_failure() => _result[0].ShouldEqual("Append failure 1: Constraint violation for event type 'FirstEvent': first failed");
    [Fact] void should_include_second_failure_concurrency_violation() => _result[1].ShouldEqual("Append failure 2: Concurrency violation");
    [Fact] void should_include_second_failure_error() => _result[2].ShouldEqual("Append failure 2: second failed");
}
