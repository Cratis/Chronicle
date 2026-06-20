// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectFailure.get_messages;

public class with_concurrency_violation : Specification
{
    string[] _result;

    void Because()
    {
        var failure = new ReactorSideEffectFailure(
            [new AppendFailure([], true, [])]);

        _result = failure.GetMessages().ToArray();
    }

    [Fact] void should_have_single_message() => _result.Length.ShouldEqual(1);
    [Fact] void should_include_concurrency_violation() => _result[0].ShouldEqual("Append failure 1: Concurrency violation");
}
