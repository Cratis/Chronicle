// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_Options.when_parsing;

public class and_only_one_payload_path_is_given : Specification
{
    Exception _missingBaseline;
    Exception _missingCurrent;

    void Because()
    {
        _missingBaseline = Catch.Exception(() => Options.Parse(["--payload-current", "payload.txt"]));
        _missingCurrent = Catch.Exception(() => Options.Parse(["--baseline", "previous.txt", "--current", "current.txt", "--payload-baseline", "payload.txt"]));
    }

    [Fact] void should_require_a_payload_baseline() => _missingBaseline.ShouldBeOfExactType<InvalidArguments>();
    [Fact] void should_require_a_payload_current_even_when_grain_contracts_are_given() => _missingCurrent.ShouldBeOfExactType<InvalidArguments>();
    [Fact] void should_explain_the_required_pair() => _missingCurrent.Message.ShouldContain("--payload-current");
}
