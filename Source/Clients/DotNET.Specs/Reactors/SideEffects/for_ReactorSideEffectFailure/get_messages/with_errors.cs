// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectFailure.get_messages;

public class with_errors : Specification
{
    string[] _result;

    void Because()
    {
        var failure = new ReactorSideEffectFailure(
            [new AppendFailure([], false, ["storage unavailable", "write concern failed"])]);

        _result = failure.GetMessages().ToArray();
    }

    [Fact] void should_have_all_errors() => _result.Length.ShouldEqual(2);
    [Fact] void should_include_first_error() => _result[0].ShouldEqual("Append failure 1: storage unavailable");
    [Fact] void should_include_second_error() => _result[1].ShouldEqual("Append failure 1: write concern failed");
}
