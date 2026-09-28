// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_Options.when_declaring_a_ceiling;

public class and_it_narrows_the_baselines : Specification
{
    Options _result;

    void Because() => _result = Options.Parse(["--major", "19", "--up-to", "19.4.9-hotfix.1", "--current", "chronicle.desc"]);

    [Fact] void should_take_the_ceiling() => _result.UpTo.ShouldEqual("19.4.9-hotfix.1");
    [Fact] void should_keep_the_major_it_narrows() => _result.Major.ShouldEqual(19);
}
