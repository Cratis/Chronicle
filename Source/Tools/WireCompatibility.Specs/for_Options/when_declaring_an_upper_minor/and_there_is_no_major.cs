// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_Options.when_declaring_an_upper_minor;

public class and_there_is_no_major : Specification
{
    Exception _exception;
    void Because() => _exception = Catch.Exception(() => Options.Parse(["--baseline", "19.4.8", "--through-minor", "4", "--current", "chronicle.desc"]));
    [Fact] void should_refuse_an_ineffective_bound() => _exception.ShouldBeOfExactType<InvalidArguments>();
}
