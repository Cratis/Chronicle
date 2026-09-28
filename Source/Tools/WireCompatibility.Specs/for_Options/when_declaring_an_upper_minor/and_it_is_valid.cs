// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_Options.when_declaring_an_upper_minor;

public class and_it_is_valid : Specification
{
    Options _result;
    void Because() => _result = Options.Parse(["--major", "19", "--through-minor", "4", "--current", "chronicle.desc"]);
    [Fact] void should_keep_the_bound() => _result.ThroughMinor.ShouldEqual(4);
}
