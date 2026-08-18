// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_Options.when_declaring_a_floor;

public class and_it_narrows_the_baselines : Specification
{
    Options _result;

    void Because() => _result = Options.Parse(["--major", "16", "--since", "16.36.0", "--current", "chronicle.desc"]);

    [Fact] void should_take_the_floor() => _result.Since.ShouldEqual("16.36.0");
    [Fact] void should_keep_the_major_it_narrows() => _result.Major.ShouldEqual(16);
}
